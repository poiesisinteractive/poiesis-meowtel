using Mono.Cecil;

var stub = ModuleDefinition.ReadModule(args[0]);
var real = new Dictionary<string, TypeDefinition>();
foreach (var path in args.Skip(1))
    foreach (var t in All(ModuleDefinition.ReadModule(path).Types)) real.TryAdd(t.FullName, t);

int issues = 0;
foreach (var st in All(stub.Types).Where(Visible).OrderBy(t => t.FullName))
{
    if (!real.TryGetValue(st.FullName, out var rt)) { Console.WriteLine($"TYPE-MISSING  {st.FullName}"); issues++; continue; }
    string Kind(TypeDefinition t) => t.IsInterface ? "interface" : t.IsEnum ? "enum" : t.IsValueType ? "struct" : t.IsAbstract && t.IsSealed ? "static class" : t.IsAbstract ? "abstract class" : t.IsSealed ? "sealed class" : "class";
    if (Kind(st) != Kind(rt)) { Console.WriteLine($"TYPE-KIND     {st.FullName}: stub {Kind(st)}, real {Kind(rt)}"); issues++; }
    if (st.BaseType != null && rt.BaseType != null && st.BaseType.FullName != rt.BaseType.FullName)
    { Console.WriteLine($"TYPE-BASE     {st.FullName}: stub : {st.BaseType.FullName}, real : {rt.BaseType.FullName}"); issues++; }
    if (st.IsEnum)
    {
        foreach (var f in st.Fields.Where(f => f.IsLiteral))
        {
            var rf = rt.Fields.FirstOrDefault(x => x.Name == f.Name);
            if (rf == null) { Console.WriteLine($"ENUM-MISSING  {st.FullName}.{f.Name}"); issues++; }
            else if (!Equals(Convert.ToInt64(rf.Constant), Convert.ToInt64(f.Constant))) { Console.WriteLine($"ENUM-VALUE    {st.FullName}.{f.Name}: stub {f.Constant}, real {rf.Constant}"); issues++; }
        }
        continue;
    }
    foreach (var m in st.Methods.Where(m => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && !m.IsGetter && !m.IsSetter && !m.IsAddOn && !m.IsRemoveOn))
    {
        var rm = FindMethod(rt, m);
        if (rm == null) { Console.WriteLine($"MEMBER-MISSING {st.FullName}::{Sig(m)}"); issues++; continue; }
        if (rm.ReturnType.FullName != m.ReturnType.FullName) { Console.WriteLine($"RETURN        {st.FullName}::{Sig(m)}: real returns {rm.ReturnType.FullName}"); issues++; }
        if (rm.IsStatic != m.IsStatic) { Console.WriteLine($"STATIC        {st.FullName}::{Sig(m)}: real static={rm.IsStatic}"); issues++; }
        if (Access(rm) != Access(m)) { Console.WriteLine($"ACCESS        {st.FullName}::{Sig(m)}: stub {Access(m)}, real {Access(rm)}"); issues++; }
        if (m.IsVirtual && !m.IsFinal && !(rm.IsVirtual && !rm.IsFinal) && !st.IsInterface) { Console.WriteLine($"VIRTUAL       {st.FullName}::{Sig(m)}: virtual in stub only"); issues++; }
        for (int i = 0; i < m.Parameters.Count; i++)
            if (m.Parameters[i].HasDefault != rm.Parameters[i].HasDefault || m.Parameters[i].IsOptional != rm.Parameters[i].IsOptional)
            { Console.WriteLine($"OPTIONAL      {st.FullName}::{Sig(m)} param {i}: stub optional={m.Parameters[i].IsOptional}, real optional={rm.Parameters[i].IsOptional}"); issues++; }
    }
    foreach (var p in st.Properties.Where(p => Vis(p.GetMethod) || Vis(p.SetMethod)))
    {
        var rp = FindProperty(rt, p.Name, p.Parameters.Count);
        if (rp == null) { Console.WriteLine($"MEMBER-MISSING {st.FullName}::{p.Name} (property)"); issues++; continue; }
        if (rp.PropertyType.FullName != p.PropertyType.FullName) { Console.WriteLine($"PROP-TYPE     {st.FullName}::{p.Name}: stub {p.PropertyType.FullName}, real {rp.PropertyType.FullName}"); issues++; }
        if (Vis(p.SetMethod) && !Vis(rp.SetMethod)) { Console.WriteLine($"PROP-SETTER   {st.FullName}::{p.Name}: settable in stub only"); issues++; }
        if (Vis(p.GetMethod) && !Vis(rp.GetMethod)) { Console.WriteLine($"PROP-GETTER   {st.FullName}::{p.Name}: readable in stub only"); issues++; }
        var sm = p.GetMethod ?? p.SetMethod; var rmm = rp.GetMethod ?? rp.SetMethod;
        if (sm != null && rmm != null && sm.IsStatic != rmm.IsStatic) { Console.WriteLine($"STATIC        {st.FullName}::{p.Name}: real static={rmm.IsStatic}"); issues++; }
    }
    foreach (var e in st.Events.Where(e => Vis(e.AddMethod)))
    {
        var re = FindEvent(rt, e.Name);
        if (re == null) { Console.WriteLine($"MEMBER-MISSING {st.FullName}::{e.Name} (event)"); issues++; continue; }
        if (re.EventType.FullName != e.EventType.FullName) { Console.WriteLine($"EVENT-TYPE    {st.FullName}::{e.Name}: stub {e.EventType.FullName}, real {re.EventType.FullName}"); issues++; }
    }
    foreach (var f in st.Fields.Where(f => f.IsPublic || f.IsFamily))
    {
        var rf = FindField(rt, f.Name);
        var rpp = rf == null ? FindProperty(rt, f.Name, 0) : null;
        if (rf == null && rpp == null) { Console.WriteLine($"MEMBER-MISSING {st.FullName}::{f.Name} (field)"); issues++; }
        else if (rf != null && rf.FieldType.FullName != f.FieldType.FullName) { Console.WriteLine($"FIELD-TYPE    {st.FullName}::{f.Name}: stub {f.FieldType.FullName}, real {rf.FieldType.FullName}"); issues++; }
    }
}
Console.WriteLine($"APIDIFF: {issues} differences");

static IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> ts) { foreach (var t in ts) { yield return t; foreach (var n in All(t.NestedTypes)) yield return n; } }
static bool Visible(TypeDefinition t) { for (var c = t; c != null; c = c.DeclaringType) if (!(c.IsPublic || c.IsNestedPublic || c.IsNestedFamily)) return false; return true; }
static bool Vis(MethodDefinition m) => m != null && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly);
static string Access(MethodDefinition m) => m.IsPublic ? "public" : m.IsFamily || m.IsFamilyOrAssembly ? "protected" : "non-public";
static string Sig(MethodDefinition m) => m.Name + (m.HasGenericParameters ? "`" + m.GenericParameters.Count : "") + "(" + string.Join(", ", m.Parameters.Select(p => p.ParameterType.FullName)) + ")";
static IEnumerable<TypeDefinition> Chain(TypeDefinition t) { for (var c = t; c != null; c = SafeResolve(c.BaseType)) yield return c; }
static TypeDefinition SafeResolve(TypeReference r) { try { return r?.Resolve(); } catch { return null; } }
static MethodDefinition FindMethod(TypeDefinition rt, MethodDefinition m) =>
    Chain(rt).SelectMany(t => t.Methods).FirstOrDefault(x => x.Name == m.Name && x.Parameters.Count == m.Parameters.Count && x.GenericParameters.Count == m.GenericParameters.Count
        && x.Parameters.Select(p => Norm(p.ParameterType.FullName)).SequenceEqual(m.Parameters.Select(p => Norm(p.ParameterType.FullName))));
static string Norm(string n) => System.Text.RegularExpressions.Regex.Replace(n, @"!!?\d+|\b[A-Z]\w*(?=[,>\]&\[]|$)", "G").Length > 0 ? n.Replace("TValue", "T").Replace("TObject", "T").Replace("T1", "T") : n;
static PropertyDefinition FindProperty(TypeDefinition rt, string name, int idx) => Chain(rt).SelectMany(t => t.Properties).FirstOrDefault(x => x.Name == name && x.Parameters.Count == idx);
static EventDefinition FindEvent(TypeDefinition rt, string name) => Chain(rt).SelectMany(t => t.Events).FirstOrDefault(x => x.Name == name);
static FieldDefinition FindField(TypeDefinition rt, string name) => Chain(rt).SelectMany(t => t.Fields).FirstOrDefault(x => x.Name == name && (x.IsPublic || x.IsFamily));
