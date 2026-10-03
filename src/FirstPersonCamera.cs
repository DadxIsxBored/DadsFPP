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
    private static readonly FieldInfo[] AttachmentFields = {
        AccessTools.Field(typeof(VisEquipment), "m_helmetItemInstance"),
        AccessTools.Field(typeof(VisEquipment), "m_hairItemInstance"),
        AccessTools.Field(typeof(VisEquipment), "m_beardItemInstance")
    };
    private readonly Dictionary<Renderer, bool> _hidden = new Dictionary<Renderer, bool>();
    private Camera? _camera;
    private Camera? _skyCamera;
    private Player? _player;
    private Transform? _hiddenHead;
    private Vector3 _headScale;
    private float _originalNearClip;
    private float _originalFov;
    private float _originalSkyFov;

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
        _player = player;
        // Vanilla still calculates rotation, aiming, input and camera shake.
        // Only replace its trailing third-person position and optical settings.
        gameCamera.transform.position = player.GetEyePoint() +
            gameCamera.transform.forward * DadsFPPPlugin.ForwardOffset.Value + Vector3.up * DadsFPPPlugin.VerticalOffset.Value;
        camera.fieldOfView = DadsFPPPlugin.FieldOfView.Value;
        camera.nearClipPlane = DadsFPPPlugin.NearClip.Value;
        if (_skyCamera != null) _skyCamera.fieldOfView = camera.fieldOfView;
    }

    internal void Restore()
    {
        RestoreVisibility();
        if (_camera != null)
        {
            _camera.nearClipPlane = _originalNearClip;
            _camera.fieldOfView = _originalFov;
        }
        if (_skyCamera != null) _skyCamera.fieldOfView = _originalSkyFov;
        _camera = null;
        _skyCamera = null;
        _player = null;
    }

    private void BeforePipelineRender(ScriptableRenderContext context, Camera camera) => BeforeRender(camera);
    private void AfterPipelineRender(ScriptableRenderContext context, Camera camera) => AfterRender(camera);

    private void BeforeRender(Camera camera)
    {
        RestoreVisibility();
        if (camera != _camera || _player == null || !DadsFPPPlugin.HideHead.Value) return;
        _hiddenHead = HeadField.GetValue(_player) as Transform;
        if (_hiddenHead != null)
        {
            _headScale = _hiddenHead.localScale;
            _hiddenHead.localScale = Vector3.zero;
        }
        VisEquipment equipment = _player.GetVisEquipment();
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
    }

    private void RestoreVisibility()
    {
        if (_hiddenHead != null) _hiddenHead.localScale = _headScale;
        _hiddenHead = null;
        foreach (KeyValuePair<Renderer, bool> entry in _hidden)
            if (entry.Key != null) entry.Key.forceRenderingOff = entry.Value;
        _hidden.Clear();
    }
}
