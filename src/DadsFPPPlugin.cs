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
    public const string PluginVersion = "1.0.0";
    internal static DadsFPPPlugin? Instance;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> FirstPerson = null!;
    internal static ConfigEntry<KeyboardShortcut> ToggleKey = null!;
    internal static ConfigEntry<float> FieldOfView = null!;
    internal static ConfigEntry<float> ForwardOffset = null!;
    internal static ConfigEntry<float> VerticalOffset = null!;
    internal static ConfigEntry<float> NearClip = null!;
    internal static ConfigEntry<bool> HideHead = null!;
    private Harmony? _harmony;
    internal readonly FirstPersonCamera Controller = new FirstPersonCamera();

    private void Awake()
    {
        Instance = this;
        Enabled = Config.Bind("1 - General", "Enabled", true, "Enable DadsFPP.");
        FirstPerson = Config.Bind("1 - General", "First Person", true, "Use first person. The toggle updates this setting.");
        ToggleKey = Config.Bind("1 - General", "Toggle Perspective", new KeyboardShortcut(KeyCode.F6), "Switch first/third person during gameplay.");
        FieldOfView = Config.Bind("2 - Camera", "Field of View", 85f, new ConfigDescription("First-person vertical FOV.", new AcceptableValueRange<float>(50f, 110f)));
        ForwardOffset = Config.Bind("2 - Camera", "Forward Offset", 0.12f, new ConfigDescription("Meters forward from the player's eyes.", new AcceptableValueRange<float>(-0.1f, 0.3f)));
        VerticalOffset = Config.Bind("2 - Camera", "Vertical Offset", 0f, new ConfigDescription("Meters above or below the player's eyes.", new AcceptableValueRange<float>(-0.3f, 0.3f)));
        NearClip = Config.Bind("2 - Camera", "Near Clip", 0.03f, new ConfigDescription("Near clipping plane in meters.", new AcceptableValueRange<float>(0.01f, 0.15f)));
        HideHead = Config.Bind("3 - Visibility", "Hide Head", true, "Hide your head, hair, beard and helmet for the world camera only. Equipment remains equipped.");
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
