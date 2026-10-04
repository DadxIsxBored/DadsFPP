using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DadsFPP;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
public sealed class DadsFPPPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.dadisbored.dadsfpp";
    public const string PluginName = "DadsFPP";
    public const string PluginVersion = "1.1.0";
    internal static DadsFPPPlugin? Instance;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> FirstPerson = null!;
    internal static ConfigEntry<KeyboardShortcut> ToggleKey = null!;
    internal static ConfigEntry<float> FieldOfView = null!;
    internal static ConfigEntry<float> ForwardOffset = null!;
    internal static ConfigEntry<float> VerticalOffset = null!;
    internal static ConfigEntry<float> NearClip = null!;
    internal static ConfigEntry<bool> HideHead = null!;
    internal static ConfigEntry<bool> ShowArmsAndWeapons = null!;
    internal static ConfigEntry<Vector3> ArmViewOffset = null!;
    private Harmony? _harmony;
    internal readonly FirstPersonCamera Controller = new FirstPersonCamera();

    private void Awake()
    {
        Instance = this;
        Enabled = Config.Bind("1 - General", "Enabled", true, "Enable DadsFPP.");
        FirstPerson = Config.Bind("1 - General", "First Person", true, "Use first person. The toggle updates this setting.");
        ToggleKey = Config.Bind("1 - General", "Toggle Perspective", new KeyboardShortcut(KeyCode.F6), "Switch first/third person during gameplay.");
        FieldOfView = Config.Bind("2 - Camera", "Field of View", 65f, new ConfigDescription("First-person vertical FOV. Valheim's default is 65 degrees; slider range is 60–110 degrees.", new AcceptableValueRange<float>(60f, 110f)));
        ForwardOffset = Config.Bind("2 - Camera", "Forward Offset", 0.12f, new ConfigDescription("Meters forward from the player's eyes.", new AcceptableValueRange<float>(-0.1f, 0.3f)));
        VerticalOffset = Config.Bind("2 - Camera", "Vertical Offset", 0f, new ConfigDescription("Meters above or below the player's eyes.", new AcceptableValueRange<float>(-0.3f, 0.3f)));
        NearClip = Config.Bind("2 - Camera", "Near Clip", 0.03f, new ConfigDescription("Near clipping plane in meters.", new AcceptableValueRange<float>(0.01f, 0.15f)));
        HideHead = Config.Bind("3 - Visibility", "Hide Head", true, "Hide your head, hair, beard and helmet for the world camera only. Equipment remains equipped.");
        ShowArmsAndWeapons = Config.Bind("3 - Visibility", "Show Arms and Weapons", true, "Align your animated arms and held weapons with the first-person view during rendering.");
        ArmViewOffset = Config.Bind("3 - Visibility", "Arm View Offset", new Vector3(0f, 0.35f, 0.45f), "Render offset for animated arms and held equipment in meters: X right, Y up, Z forward. Applies only to the first-person camera.");
        Controller.Subscribe();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(DadsFPPPlugin).Assembly);
        Logger.LogInfo($"{PluginName} {PluginVersion} loaded. F6 toggles perspective by default.");
    }

    private void Update()
    {
        if (!Enabled.Value || Player.m_localPlayer == null || !GameplayInputAvailable()) return;
        KeyboardShortcut shortcut = ToggleKey.Value;
        if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey)) return;
        foreach (KeyCode modifier in shortcut.Modifiers) if (!Input.GetKey(modifier)) return;
        FirstPerson.Value = !FirstPerson.Value;
        if (!FirstPerson.Value) Controller.Restore();
    }

    internal static bool GameplayInputAvailable() => Cursor.lockState == CursorLockMode.Locked &&
        !InventoryGui.IsVisible() && !Menu.IsVisible() && !Console.IsVisible() &&
        !TextInput.IsVisible() && !Minimap.IsOpen() && !StoreGui.IsVisible() &&
        !(Chat.instance != null && Chat.instance.HasFocus());

    private void OnDestroy()
    {
        Controller.Unsubscribe();
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}

[HarmonyPatch(typeof(GameCamera), "UpdateCamera", new[] { typeof(float) })]
internal static class CameraUpdatePatch
{
    [HarmonyPostfix, HarmonyPriority(Priority.Last)]
    private static void Postfix(GameCamera __instance, Camera ___m_camera, bool ___m_freeFly)
    {
        DadsFPPPlugin.Instance?.Controller.Update(__instance, ___m_camera, Player.m_localPlayer, ___m_freeFly);
    }
}
