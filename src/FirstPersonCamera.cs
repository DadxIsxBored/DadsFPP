using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace DadsFPP;

internal sealed class FirstPersonCamera
{
    private static readonly FieldInfo HeadField = AccessTools.Field(typeof(Character), "m_head");
    private static readonly Action<Character, bool> SetCharacterVisible = AccessTools.MethodDelegate<Action<Character, bool>>(
        AccessTools.Method(typeof(Character), "SetVisible", new[] { typeof(bool) }));
    private static readonly FieldInfo LeftHeldItemField = AccessTools.Field(typeof(VisEquipment), "m_leftItemInstance");
    private static readonly FieldInfo RightHeldItemField = AccessTools.Field(typeof(VisEquipment), "m_rightItemInstance");
    private static readonly FieldInfo[] AttachmentFields = {
        AccessTools.Field(typeof(VisEquipment), "m_helmetItemInstance"),
        AccessTools.Field(typeof(VisEquipment), "m_hairItemInstance"),
        AccessTools.Field(typeof(VisEquipment), "m_beardItemInstance")
    };
    private readonly Dictionary<Renderer, bool> _hidden = new Dictionary<Renderer, bool>();
    private readonly Dictionary<Transform, ArmPose> _armPoses = new Dictionary<Transform, ArmPose>();
    private readonly Dictionary<SkinnedMeshRenderer, bool> _skinUpdates = new Dictionary<SkinnedMeshRenderer, bool>();
    private readonly HashSet<Camera> _suspendedCameras = new HashSet<Camera>();
    private GameObject? _heldItem;
    private Vector3 _gripAdjustment;
    private Animator? _animator;
    private AnimatorCullingMode _originalCullingMode;
    private Transform? _leftArm;
    private Transform? _rightArm;
    private Player? _armPlayer;
    private Camera? _camera;
    private Camera? _skyCamera;
    private Player? _player;
    private Transform? _hiddenHead;
    private Vector3 _headScale;
    private float _originalNearClip;
    private float _originalFov;
    private float _originalSkyFov;

    internal bool IsActiveFor(Character character) => _camera != null && _player != null &&
        _player == character && DadsFPPPlugin.Enabled.Value && DadsFPPPlugin.FirstPerson.Value;

    private readonly struct ArmPose
    {
        internal readonly Vector3 Position;
        internal readonly Quaternion Rotation;

        internal ArmPose(Transform arm)
        {
            Position = arm.localPosition;
            Rotation = arm.localRotation;
        }
    }

    internal void Subscribe()
    {
        Camera.onPreCull += BeforeRender;
        Camera.onPostRender += AfterRender;
        RenderPipelineManager.beginCameraRendering += BeforePipelineRender;
        RenderPipelineManager.endCameraRendering += AfterPipelineRender;
    }

    internal void Unsubscribe()
    {
        Restore();
        Camera.onPreCull -= BeforeRender;
        Camera.onPostRender -= AfterRender;
        RenderPipelineManager.beginCameraRendering -= BeforePipelineRender;
        RenderPipelineManager.endCameraRendering -= AfterPipelineRender;
    }

    internal void Update(GameCamera gameCamera, Camera camera, Player? player, bool freeFly)
    {
        RestoreVisibility();
        if (!DadsFPPPlugin.Enabled.Value || !DadsFPPPlugin.FirstPerson.Value || camera == null ||
            player == null || player.m_eye == null || freeFly || player.IsDead() || player.InCutscene() ||
            player.IsTeleporting() || InventoryGui.IsVisible() || player.IsAttached())
        {
            Restore();
            return;
        }
        if (_camera != camera)
        {
            Restore();
            _camera = camera;
            _skyCamera = gameCamera.m_skyCamera;
            _originalNearClip = camera.nearClipPlane;
            _originalFov = camera.fieldOfView;
            if (_skyCamera != null) _originalSkyFov = _skyCamera.fieldOfView;
        }
        if (_player != player)
        {
            RestoreAnimationUpdates();
            _player = player;
            _animator = player.GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _originalCullingMode = _animator.cullingMode;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            foreach (SkinnedMeshRenderer renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                _skinUpdates.Add(renderer, renderer.updateWhenOffscreen);
                renderer.updateWhenOffscreen = true;
            }
        }
        // Vanilla still calculates rotation, aiming, input and camera shake.
        // Only replace its trailing third-person position and optical settings.
        gameCamera.transform.position = player.GetEyePoint() +
            gameCamera.transform.forward * DadsFPPPlugin.ForwardOffset.Value + Vector3.up * DadsFPPPlugin.VerticalOffset.Value;
        camera.fieldOfView = DadsFPPPlugin.FieldOfView.Value;
        camera.nearClipPlane = DadsFPPPlugin.NearClip.Value;
        if (_skyCamera != null) _skyCamera.fieldOfView = camera.fieldOfView;
        // Player.FixedUpdate normally hides the entire LOD group when the
        // camera is within two meters, which also fades held tools away.
        SetCharacterVisible(player, true);
        UpdateToolGrip(camera, player);
    }

    internal void Restore()
    {
        RestoreVisibility();
        RestoreAnimationUpdates();
        if (_camera != null)
        {
            _camera.nearClipPlane = _originalNearClip;
            _camera.fieldOfView = _originalFov;
        }
        if (_skyCamera != null) _skyCamera.fieldOfView = _originalSkyFov;
        _camera = null;
        _skyCamera = null;
        _player = null;
        _armPlayer = null;
        _leftArm = null;
        _rightArm = null;
        _heldItem = null;
        _gripAdjustment = Vector3.zero;
        _suspendedCameras.Clear();
    }

    private void BeforePipelineRender(ScriptableRenderContext context, Camera camera) => BeforeRender(camera);
    private void AfterPipelineRender(ScriptableRenderContext context, Camera camera) => AfterRender(camera);

    private void BeforeRender(Camera camera)
    {
        if (camera != _camera)
        {
            if (_armPoses.Count > 0 || _hiddenHead != null || _hidden.Count > 0)
            {
                RestoreVisibility();
                _suspendedCameras.Add(camera);
            }
            return;
        }
        RestoreVisibility();
        if (camera != _camera || _player == null) return;
        VisEquipment equipment = _player.GetVisEquipment();
        if (DadsFPPPlugin.ShowArmsAndWeapons.Value && equipment != null)
        {
            if (_armPlayer != _player || _leftArm == null || _rightArm == null)
            {
                _armPlayer = _player;
                _leftArm = FindUpperArm(equipment.m_leftHand);
                _rightArm = FindUpperArm(equipment.m_rightHand);
            }
            // Use the live animated bones. Their held equipment follows the same
            // pose, so swings, bow draws, blocking and tool use remain animated.
            Quaternion viewRotation = Quaternion.FromToRotation(_player.transform.forward, camera.transform.forward);
            Vector3 eyePoint = _player.GetEyePoint();
            Vector3 offset = camera.transform.TransformDirection(DadsFPPPlugin.ArmViewOffset.Value + _gripAdjustment);
            PoseArm(_leftArm, eyePoint, viewRotation, offset);
            PoseArm(_rightArm, eyePoint, viewRotation, offset);
        }
        if (!DadsFPPPlugin.HideHead.Value) return;
        _hiddenHead = HeadField.GetValue(_player) as Transform;
        if (_hiddenHead != null)
        {
            _headScale = _hiddenHead.localScale;
            _hiddenHead.localScale = Vector3.zero;
        }
        if (equipment == null) return;
        foreach (FieldInfo field in AttachmentFields)
        {
            GameObject? attachment = field.GetValue(equipment) as GameObject;
            if (attachment == null) continue;
            foreach (Renderer renderer in attachment.GetComponentsInChildren<Renderer>(true))
            {
                if (_hidden.ContainsKey(renderer)) continue;
                _hidden.Add(renderer, renderer.forceRenderingOff);
                renderer.forceRenderingOff = true;
            }
        }
    }

    private void AfterRender(Camera camera)
    {
        if (camera == _camera) RestoreVisibility();
        else if (_suspendedCameras.Remove(camera) && _camera != null) BeforeRender(_camera);
    }

    private void UpdateToolGrip(Camera camera, Player player)
    {
        VisEquipment equipment = player.GetVisEquipment();
        if (!DadsFPPPlugin.ShowArmsAndWeapons.Value || equipment == null)
        {
            _heldItem = null;
            _gripAdjustment = Vector3.zero;
            return;
        }
        GameObject? rightItem = RightHeldItemField.GetValue(equipment) as GameObject;
        GameObject? leftItem = LeftHeldItemField.GetValue(equipment) as GameObject;
        GameObject? heldItem = rightItem != null && rightItem.activeInHierarchy ? rightItem : leftItem;
        Transform? hand = heldItem == rightItem ? equipment.m_rightHand : equipment.m_leftHand;
        if (heldItem == null || !heldItem.activeInHierarchy || hand == null)
        {
            _heldItem = null;
            _gripAdjustment = Vector3.zero;
            return;
        }
        bool changedItem = _heldItem != heldItem;
        _heldItem = heldItem;
        // Calibrate against the actual tool grip at rest, then keep this offset
        // through the action so animated swings and draws retain their motion.
        if (!changedItem && (player.InAttack() || player.IsBlocking() || player.IsDrawingBow() || player.InDodge())) return;
        Quaternion viewRotation = Quaternion.FromToRotation(player.transform.forward, camera.transform.forward);
        Vector3 eyePoint = player.GetEyePoint();
        Vector3 grip = camera.transform.InverseTransformPoint(eyePoint + viewRotation * (hand.position - eyePoint)) +
            DadsFPPPlugin.ArmViewOffset.Value;
        ToolGripFraming.Fit(grip.x, grip.y, grip.z, camera.fieldOfView, camera.aspect,
            out float visibleX, out float visibleY, out float visibleZ);
        Vector3 visibleGrip = new Vector3(visibleX, visibleY, visibleZ);
        _gripAdjustment = visibleGrip - grip;
    }

    private static Transform? FindUpperArm(Transform? hand)
    {
        for (Transform? bone = hand; bone != null; bone = bone.parent)
        {
            string name = bone.name.Replace("_", "").Replace(" ", "").ToLowerInvariant();
            if (name.Contains("upperarm") || name.EndsWith("leftarm") || name.EndsWith("rightarm") ||
                name == "arml" || name == "armr" || name == "larm" || name == "rarm")
                return bone;
        }
        return null;
    }

    private void PoseArm(Transform? arm, Vector3 eyePoint, Quaternion viewRotation, Vector3 offset)
    {
        if (arm == null || _armPoses.ContainsKey(arm)) return;
        _armPoses.Add(arm, new ArmPose(arm));
        Vector3 position = arm.position;
        Quaternion rotation = arm.rotation;
        arm.position = eyePoint + viewRotation * (position - eyePoint) + offset;
        arm.rotation = viewRotation * rotation;
    }

    private void RestoreVisibility()
    {
        foreach (KeyValuePair<Transform, ArmPose> entry in _armPoses)
        {
            if (entry.Key == null) continue;
            entry.Key.localPosition = entry.Value.Position;
            entry.Key.localRotation = entry.Value.Rotation;
        }
        _armPoses.Clear();
        if (_hiddenHead != null) _hiddenHead.localScale = _headScale;
        _hiddenHead = null;
        foreach (KeyValuePair<Renderer, bool> entry in _hidden)
            if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
        _hidden.Clear();
    }

    private void RestoreAnimationUpdates()
    {
        if (_animator != null) _animator.cullingMode = _originalCullingMode;
        _animator = null;
        foreach (KeyValuePair<SkinnedMeshRenderer, bool> entry in _skinUpdates)
            if (entry.Key != null) entry.Key.updateWhenOffscreen = entry.Value;
        _skinUpdates.Clear();
    }
}
