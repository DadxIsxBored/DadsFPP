using Mono.Cecil;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 3) { Console.WriteLine("Usage: ApiChecks <plugin DLL> <game Managed directory> <BepInEx core directory>"); return 1; }
        try { Check(args); return 0; }
        catch (Exception e) { Console.WriteLine(e); return 1; }
    }

    private static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    private static void Check(string[] args)
    {
        using var plugin = AssemblyDefinition.ReadAssembly(args[0]);
        using var game = AssemblyDefinition.ReadAssembly(Path.Combine(args[1], "assembly_valheim.dll"));
        var controller = plugin.MainModule.GetType("DadsFPP.FirstPersonCamera");
        var initializers = controller.Methods.Single(m => m.Name == ".cctor").Body.Instructions;
        bool FieldMatches(string type, string name, string fieldType) => game.MainModule.GetType(type).Fields.Any(f => f.Name == name && f.FieldType.FullName == fieldType);
        Assert(FieldMatches("Character", "m_head", "UnityEngine.Transform"), "Head field exists on the installed original game assembly");
        foreach (string name in new[] { "m_head", "m_helmetItemInstance", "m_hairItemInstance", "m_beardItemInstance" })
        {
            Assert(initializers.Any(i => Equals(i.Operand, name)), "Plugin reflection initializer requests " + name);
            if (name != "m_head") Assert(FieldMatches("VisEquipment", name, "UnityEngine.GameObject"), "Game visual attachment matches " + name);
        }
        var camera = game.MainModule.GetType("GameCamera");
        Assert(camera.Methods.Any(m => m.Name == "UpdateCamera" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.Single" && m.ReturnType.FullName == "System.Void"), "UpdateCamera(float) patch target resolves");
        Assert(FieldMatches("GameCamera", "m_camera", "UnityEngine.Camera") && FieldMatches("GameCamera", "m_freeFly", "System.Boolean"), "Injected camera and free-fly fields match");
        var equipment = game.MainModule.GetType("VisEquipment");
        foreach (string name in new[] { "m_leftHand", "m_rightHand" })
            Assert(FieldMatches("VisEquipment", name, "UnityEngine.Transform") && equipment.Fields.Single(f => f.Name == name).IsPublic,
                "Public animated hand attachment resolves: " + name);
        Assert(plugin.MainModule.GetMemberReferences().OfType<FieldReference>()
            .Where(f => f.DeclaringType.FullName == "VisEquipment")
            .All(f => equipment.Fields.Any(original => original.Name == f.Name && original.IsPublic)),
            "Private visual attachments are not accessed directly");
    }
}
