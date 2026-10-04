using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DadsFPP;

internal sealed class HealthReadout
{
    private readonly List<Entry> _entries = new List<Entry>();
    private GUIStyle? _label;

    private readonly struct Entry
    {
        internal readonly string Text;
        internal readonly Color Color;
        internal readonly float Expires;

        internal Entry(float change)
        {
            bool healed = change > 0f;
            Text = (healed ? "Healed +" : "Damage -") +
                Mathf.Abs(change).ToString("0.##", CultureInfo.InvariantCulture);
            Color = healed ? new Color(0.45f, 1f, 0.5f) : new Color(1f, 0.4f, 0.35f);
            Expires = Time.unscaledTime + 4f;
        }
    }

    internal void Record(float change)
    {
        if (float.IsNaN(change) || float.IsInfinity(change) || Mathf.Abs(change) < 0.001f) return;
        _entries.RemoveAll(entry => entry.Expires <= Time.unscaledTime);
        _entries.Insert(0, new Entry(change));
        if (_entries.Count > 3) _entries.RemoveAt(3);
    }

    internal void Draw(FirstPersonCamera controller)
    {
        Player player = Player.m_localPlayer;
        if (player == null || !controller.IsActiveFor(player))
        {
            _entries.Clear();
            return;
        }
        _entries.RemoveAll(entry => entry.Expires <= Time.unscaledTime);
        if (_entries.Count == 0 || Hud.instance == null || !Hud.instance.IsVisible()) return;
        if (Event.current.type != EventType.Repaint) return;
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _label.normal.textColor = Color.white;
        }

        float scale = Mathf.Clamp(Screen.height / 1080f, 0.65f, 2f);
        float size = 160f * scale;
        Rect square = new Rect(Screen.width - size - 24f * scale, (Screen.height - size) * 0.5f, size, size);
        Color originalColor = GUI.color;
        try
        {
            GUI.color = new Color(0.75f, 0.75f, 0.75f, 0.85f);
            GUI.DrawTexture(square, Texture2D.whiteTexture);
            GUI.color = new Color(0.035f, 0.035f, 0.035f, 0.9f);
            GUI.DrawTexture(new Rect(square.x + scale, square.y + scale, size - 2f * scale, size - 2f * scale), Texture2D.whiteTexture);
            _label.fontSize = Mathf.RoundToInt(19f * scale);
            float rowHeight = 42f * scale;
            float top = square.y + (size - rowHeight * _entries.Count) * 0.5f;
            for (int index = 0; index < _entries.Count; index++)
            {
                GUI.color = _entries[index].Color;
                GUI.Label(new Rect(square.x + 6f * scale, top + index * rowHeight, size - 12f * scale, rowHeight),
                    _entries[index].Text, _label);
            }
        }
        finally { GUI.color = originalColor; }
    }
}

[HarmonyPatch]
internal static class HealthChangePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Character), "ApplyDamage",
            new[] { typeof(HitData), typeof(bool), typeof(bool), typeof(HitData.DamageModifier) });
        yield return AccessTools.Method(typeof(Character), "RPC_Heal", new[] { typeof(long), typeof(float), typeof(bool) });
    }

    private static void Prefix(Character __instance, out float __state)
    {
        __state = DadsFPPPlugin.Instance?.Controller.IsActiveFor(__instance) == true
            ? Mathf.Max(0f, __instance.GetHealth()) : float.NaN;
    }

    private static void Postfix(Character __instance, float __state)
    {
        if (float.IsNaN(__state) || DadsFPPPlugin.Instance?.Controller.IsActiveFor(__instance) != true) return;
        DadsFPPPlugin.Instance.HealthNumbers.Record(Mathf.Max(0f, __instance.GetHealth()) - __state);
    }
}
