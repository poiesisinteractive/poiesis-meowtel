// UnityRefPatcher: produces the UnityEngine reference DLLs used by the harness.
//
// Input : UnityEngine.Modules 2021.3.33 (nuget.org) - these DLLs are PUBLICIZED (every private/internal/protected
//         member was made public), which would let wrong code type-check.
// Oracle: Unity3D.SDK 2021.1.14.1 (nuget.org) monolithic UnityEngine.dll, with the real accessibility.
// Output: lib/unity-patched/*.dll where
//   1. accessibility (types, methods, fields) is restored from the oracle for every member present in 2021.1;
//      members added in 2021.2/2021.3 keep their public flag unless a naming heuristic says they are internal;
//   2. a small table of Unity 6-only MEMBERS is injected into existing types (they cannot be added from C# source);
//   3. [Obsolete] (warning) markers are added on APIs that Unity 6 deprecated.
// Usage: UnityRefPatcher <inDir> <oracleUnityEngine.dll> <outDir>
using Mono.Cecil;
using Mono.Cecil.Cil;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 3) { Console.Error.WriteLine("usage: UnityRefPatcher <inDir> <oracle UnityEngine.dll> <outDir>"); return 2; }
        string inDir = args[0], oraclePath = args[1], outDir = args[2];
        Directory.CreateDirectory(outDir);

        var oracle = ModuleDefinition.ReadModule(oraclePath);
        var oTypes = new Dictionary<string, TypeDefinition>();
        foreach (var t in AllTypes(oracle.Types)) oTypes[t.FullName] = t;

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(inDir);
        var rp = new ReaderParameters { AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate };

        int restored = 0, unmatchedTypes = 0, unmatchedMembers = 0, heuristic = 0;
        var unmatchedPublicTypes = new List<string>();
        foreach (var path in Directory.GetFiles(inDir, "UnityEngine*.dll").OrderBy(p => p))
        {
            var module = ModuleDefinition.ReadModule(path, rp);
            foreach (var t in AllTypes(module.Types))
            {
                if (oTypes.TryGetValue(t.FullName, out var ot))
                {
                    if ((t.Attributes & TypeAttributes.VisibilityMask) != (ot.Attributes & TypeAttributes.VisibilityMask)) restored++;
                    t.Attributes = (t.Attributes & ~TypeAttributes.VisibilityMask) | (ot.Attributes & TypeAttributes.VisibilityMask);
                    var oMethods = new Dictionary<string, MethodDefinition>();
                    foreach (var om in ot.Methods) oMethods[MethodKey(om)] = om;
                    var oFields = ot.Fields.ToDictionary(f => f.Name, f => f);
                    foreach (var m in t.Methods)
                    {
                        if (oMethods.TryGetValue(MethodKey(m), out var om))
                        {
                            if ((m.Attributes & MethodAttributes.MemberAccessMask) != (om.Attributes & MethodAttributes.MemberAccessMask)) restored++;
                            m.Attributes = (m.Attributes & ~MethodAttributes.MemberAccessMask) | (om.Attributes & MethodAttributes.MemberAccessMask);
                        }
                        else { unmatchedMembers++; if (HeuristicMethod(m)) heuristic++; }
                    }
                    foreach (var f in t.Fields)
                    {
                        if (oFields.TryGetValue(f.Name, out var of))
                        {
                            if ((f.Attributes & FieldAttributes.FieldAccessMask) != (of.Attributes & FieldAttributes.FieldAccessMask)) restored++;
                            f.Attributes = (f.Attributes & ~FieldAttributes.FieldAccessMask) | (of.Attributes & FieldAttributes.FieldAccessMask);
                            if (of.IsInitOnly) f.IsInitOnly = true;
                        }
                        else { unmatchedMembers++; if (HeuristicField(f)) heuristic++; }
                    }
                }
                else
                {
                    unmatchedTypes++;
                    if (HeuristicType(t)) heuristic++;
                    else if (IsVisible(t)) unmatchedPublicTypes.Add(module.Name + " " + t.FullName);
                    foreach (var m in t.Methods) if (HeuristicMethod(m)) heuristic++;
                    foreach (var f in t.Fields) if (HeuristicField(f)) heuristic++;
                }
            }

            Unity6.Apply(module);

            module.Write(Path.Combine(outDir, Path.GetFileName(path)));
        }

        File.WriteAllLines(Path.Combine(outDir, "unmatched-public-types.txt"), unmatchedPublicTypes);
        Console.WriteLine($"UnityRefPatcher: restored {restored} accessibilities, {unmatchedTypes} types / {unmatchedMembers} members not in oracle, {heuristic} hidden by heuristic, {unmatchedPublicTypes.Count} new public types kept (see unmatched-public-types.txt)");
        return 0;
    }

    static bool IsVisible(TypeDefinition t)
    {
        for (var c = t; c != null; c = c.DeclaringType)
            if (!(c.IsPublic || c.IsNestedPublic)) return false;
        return true;
    }

    static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var t in types)
        {
            yield return t;
            foreach (var n in AllTypes(t.NestedTypes)) yield return n;
        }
    }

    static string MethodKey(MethodDefinition m) =>
        m.Name + "`" + m.GenericParameters.Count + "(" + string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) + "):" + m.ReturnType.FullName + (m.IsStatic ? "s" : "i");

    // Names Unity uses for non-public API. Only applied to members that are absent from the 2021.1 oracle.
    static bool HeuristicType(TypeDefinition t)
    {
        bool hidden = t.Name.Contains('<') || t.Name.StartsWith("__") || t.Name.EndsWith("_Injected")
            || t.Namespace.StartsWith("UnityEngine.Bindings") || t.Namespace.StartsWith("UnityEngineInternal")
            || (t.Namespace.Contains(".Internal") || t.Name.StartsWith("Internal"))
            || t.Name.EndsWith("BindingsHelper") || t.Name.EndsWith("Bindings");
        if (!hidden) return false;
        if (t.IsNested) t.Attributes = (t.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedAssembly;
        else t.Attributes = (t.Attributes & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;
        return true;
    }

    static bool HeuristicMethod(MethodDefinition m)
    {
        if (m.Name.Contains('<') || m.Name.EndsWith("_Injected") || m.Name.StartsWith("Internal") || m.Name.StartsWith("INTERNAL_")
            || m.Name.StartsWith("get_Internal") || m.Name.StartsWith("set_Internal") || m.Name.StartsWith("get_internal") || m.Name.StartsWith("set_internal"))
        {
            m.Attributes = (m.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
            return true;
        }
        return false;
    }

    static bool HeuristicField(FieldDefinition f)
    {
        if (f.Name.Contains('<') || f.Name.StartsWith("m_") || f.Name.StartsWith("s_") || f.Name.StartsWith("k_") || f.Name.StartsWith("_"))
        {
            f.Attributes = (f.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Assembly;
            return true;
        }
        return false;
    }
}

// Unity 6 (6000.x) members that do not exist in 2021.3 and cannot be expressed as C# extension members (C# 9).
// Each entry is a BELIEF about the Unity 6 API; keep this table small and documented in README.md.
static class Unity6
{
    public static void Apply(ModuleDefinition module)
    {
        var ts = module.TypeSystem;
        TypeReference Bcl(string ns, string name, bool valueType) => new TypeReference(ns, name, module, ts.CoreLibrary, valueType);
        TypeReference Core(string name, bool valueType)
        {
            var local = module.GetType("UnityEngine." + name);
            if (local != null) return local;
            var scope = module.AssemblyReferences.FirstOrDefault(a => a.Name == "UnityEngine.CoreModule")
                        ?? throw new InvalidOperationException(module.Name + " does not reference UnityEngine.CoreModule");
            return new TypeReference("UnityEngine", name, module, scope, valueType);
        }
        var cancellationToken = Bcl("System.Threading", "CancellationToken", true);

        switch (module.Assembly.Name.Name)
        {
            case "UnityEngine.CoreModule":
            {
                var mb = module.GetType("UnityEngine.MonoBehaviour");
                AddProperty(module, mb, "destroyCancellationToken", cancellationToken, isStatic: false, setter: false); // Unity 2022.2+
                var app = module.GetType("UnityEngine.Application");
                AddProperty(module, app, "exitCancellationToken", cancellationToken, isStatic: true, setter: false);   // Unity 2022.2+
                var qs = module.GetType("UnityEngine.QualitySettings");
                AddProperty(module, qs, "count", ts.Int32, isStatic: true, setter: false);                         // Unity 2022.2+ (used by QualityAdapter.cs)

                // RuntimePlatform members added after 2021.3 (referenced by com.unity.services.analytics 6.3.0).
                // GUESS: numeric values (picked above the 2021.3 maximum so switch labels never collide).
                var rp = module.GetType("UnityEngine.RuntimePlatform");
                AddEnumValue(rp, "VisionOS");
                AddEnumValue(rp, "Switch2");

                var obj = module.GetType("UnityEngine.Object");
                // Unity 6000.2+: EntityId + Object.GetEntityId() (referenced by com.unity.addressables 2.8.1 under UNITY_6000_3_OR_NEWER).
                // GUESS: EntityId is modelled as an opaque struct (its conversion operators are not reproduced).
                var entityId = module.GetType("UnityEngine.EntityId");
                if (entityId == null)
                {
                    entityId = new TypeDefinition("UnityEngine", "EntityId",
                        TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.BeforeFieldInit,
                        Bcl("System", "ValueType", false));
                    entityId.Fields.Add(new FieldDefinition("m_Value", FieldAttributes.Private, ts.Int32));
                    module.Types.Add(entityId);
                }
                AddMethod(obj, "GetEntityId", entityId, isStatic: false);
                foreach (var m in obj.Methods.Where(m => m.Name == "FindObjectOfType"))
                    Obsolete(module, m, "Object.FindObjectOfType has been deprecated. Use Object.FindFirstObjectByType instead or if finding any instance is acceptable the faster Object.FindAnyObjectByType");
                foreach (var m in obj.Methods.Where(m => m.Name == "FindObjectsOfType"))
                    Obsolete(module, m, "Object.FindObjectsOfType has been deprecated. Use Object.FindObjectsByType instead which lets you decide whether you need the results sorted or not.  FindObjectsOfType sorts the results by InstanceID but if you do not need this using FindObjectSortMode.None is considerably faster.");
                break;
            }
            case "UnityEngine.AssetBundleModule":
            {
                // Unity 2023.1+ (referenced by com.unity.addressables 2.8.1 ResourceManager).
                var bundle = module.GetType("UnityEngine.AssetBundle");
                var op = module.GetType("UnityEngine.AssetBundleUnloadOperation");
                if (op == null)
                {
                    op = new TypeDefinition("UnityEngine", "AssetBundleUnloadOperation",
                        TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.BeforeFieldInit, Core("AsyncOperation", false));
                    module.Types.Add(op);
                    AddMethod(op, "WaitForCompletion", ts.Void, isStatic: false);
                }
                // 2021.3 already has UnloadAsync(bool) returning AsyncOperation: Unity 2023.1 narrowed the return type.
                var unloadAsync = bundle.Methods.FirstOrDefault(m => m.Name == "UnloadAsync" && m.Parameters.Count == 1);
                if (unloadAsync != null) unloadAsync.ReturnType = op;
                else AddMethod(bundle, "UnloadAsync", op, isStatic: false, ("unloadAllLoadedObjects", ts.Boolean));
                break;
            }
            case "UnityEngine.UnityWebRequestModule":
            {
                // Unity 2022.2+ (referenced by com.unity.services.authentication 3.6.1 under UNITY_2022_2_OR_NEWER).
                var uwr = module.GetType("UnityEngine.Networking.UnityWebRequest");
                // reuse the module's own System.Uri reference (it lives in "System", not in the core library)
                var uri = uwr.Methods.SelectMany(m => m.Parameters).Select(p => p.ParameterType).First(t => t.FullName == "System.Uri");
                AddStaticMethod(uwr, "PostWwwForm", uwr, ("uri", ts.String), ("form", ts.String));
                AddStaticMethod(uwr, "PostWwwForm", uwr, ("uri", uri), ("form", ts.String));
                AddStaticMethod(uwr, "Post", uwr, ("uri", ts.String), ("postData", ts.String), ("contentType", ts.String));
                AddStaticMethod(uwr, "Post", uwr, ("uri", uri), ("postData", ts.String), ("contentType", ts.String));
                break;
            }
            case "UnityEngine.Physics2DModule":
            {
                var rb = module.GetType("UnityEngine.Rigidbody2D");
                var v2 = Core("Vector2", true);
                AddProperty(module, rb, "linearVelocity", v2, false, true);
                AddProperty(module, rb, "linearVelocityX", ts.Single, false, true);
                AddProperty(module, rb, "linearVelocityY", ts.Single, false, true);
                AddProperty(module, rb, "linearDamping", ts.Single, false, true);
                AddProperty(module, rb, "angularDamping", ts.Single, false, true);
                ObsoleteProperty(module, rb, "velocity", "velocity has been deprecated. Use linearVelocity instead. (UnityUpgradable) -> linearVelocity");
                ObsoleteProperty(module, rb, "drag", "drag has been deprecated. Use linearDamping instead. (UnityUpgradable) -> linearDamping");
                ObsoleteProperty(module, rb, "angularDrag", "angularDrag has been deprecated. Use angularDamping instead. (UnityUpgradable) -> angularDamping");
                ObsoleteProperty(module, rb, "isKinematic", "isKinematic has been deprecated. Please use bodyType."); // GUESS: exact message
                break;
            }
            case "UnityEngine.PhysicsModule":
            {
                var rb = module.GetType("UnityEngine.Rigidbody");
                var v3 = Core("Vector3", true);
                AddProperty(module, rb, "linearVelocity", v3, false, true);
                AddProperty(module, rb, "linearDamping", ts.Single, false, true);
                AddProperty(module, rb, "angularDamping", ts.Single, false, true);
                ObsoleteProperty(module, rb, "velocity", "velocity has been deprecated. Use linearVelocity instead. (UnityUpgradable) -> linearVelocity");
                ObsoleteProperty(module, rb, "drag", "drag has been deprecated. Use linearDamping instead. (UnityUpgradable) -> linearDamping");
                ObsoleteProperty(module, rb, "angularDrag", "angularDrag has been deprecated. Use angularDamping instead. (UnityUpgradable) -> angularDamping");
                break;
            }
        }
    }

    static void ThrowBody(MethodDefinition m)
    {
        var il = m.Body.GetILProcessor();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Throw);
    }

    static void AddProperty(ModuleDefinition module, TypeDefinition t, string name, TypeReference type, bool isStatic, bool setter)
    {
        if (t.Properties.Any(p => p.Name == name)) return; // already there (newer reference DLLs)
        var attrs = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | (isStatic ? MethodAttributes.Static : 0);
        var get = new MethodDefinition("get_" + name, attrs, type) { HasThis = !isStatic };
        ThrowBody(get);
        t.Methods.Add(get);
        var prop = new PropertyDefinition(name, PropertyAttributes.None, type) { GetMethod = get, HasThis = !isStatic };
        if (setter)
        {
            var set = new MethodDefinition("set_" + name, attrs, module.TypeSystem.Void) { HasThis = !isStatic };
            set.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, type));
            ThrowBody(set);
            t.Methods.Add(set);
            prop.SetMethod = set;
        }
        t.Properties.Add(prop);
    }

    static void AddStaticMethod(TypeDefinition t, string name, TypeReference returnType, params (string name, TypeReference type)[] parameters)
        => AddMethod(t, name, returnType, true, parameters);

    static void AddMethod(TypeDefinition t, string name, TypeReference returnType, bool isStatic, params (string name, TypeReference type)[] parameters)
    {
        bool exists = t.Methods.Any(m => m.Name == name && m.Parameters.Count == parameters.Length
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.type.FullName)));
        if (exists) return;
        var m = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig | (isStatic ? MethodAttributes.Static : 0), returnType) { HasThis = !isStatic };
        foreach (var (pn, pt) in parameters) m.Parameters.Add(new ParameterDefinition(pn, ParameterAttributes.None, pt));
        ThrowBody(m);
        t.Methods.Add(m);
    }

    static void AddEnumValue(TypeDefinition enumType, string name)
    {
        if (enumType.Fields.Any(f => f.Name == name)) return;
        int max = enumType.Fields.Where(f => f.IsStatic && f.HasConstant).Select(f => Convert.ToInt32(f.Constant)).DefaultIfEmpty(0).Max();
        var field = new FieldDefinition(name, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, enumType)
        { Constant = max + 1 };
        enumType.Fields.Add(field);
    }

    static CustomAttribute ObsoleteAttr(ModuleDefinition module, string message)
    {
        var ts = module.TypeSystem;
        var attrType = new TypeReference("System", "ObsoleteAttribute", module, ts.CoreLibrary, false);
        var ctor = new MethodReference(".ctor", ts.Void, attrType) { HasThis = true };
        ctor.Parameters.Add(new ParameterDefinition(ts.String));
        var ca = new CustomAttribute(ctor);
        ca.ConstructorArguments.Add(new CustomAttributeArgument(ts.String, message));
        return ca;
    }

    static void Obsolete(ModuleDefinition module, ICustomAttributeProvider member, string message)
    {
        if (member.CustomAttributes.Any(a => a.AttributeType.FullName == "System.ObsoleteAttribute")) return;
        member.CustomAttributes.Add(ObsoleteAttr(module, message));
    }

    static void ObsoleteProperty(ModuleDefinition module, TypeDefinition t, string name, string message)
    {
        var p = t.Properties.FirstOrDefault(x => x.Name == name);
        if (p != null) Obsolete(module, p, message);
    }
}
