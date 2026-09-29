// RefInspect: prints types, methods and fields (with accessibility) of an assembly, e.g. to check whether a UnityEngine
// member exists in the harness reference DLLs: dotnet tools/RefInspect/out/RefInspect.dll lib/unity-patched/UnityEngine.CoreModule.dll UnityEngine.QualitySettings
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
var path = args[0]; var typeFilter = args.Length > 1 ? args[1] : null;
using var fs = File.OpenRead(path);
using var pe = new PEReader(fs);
var md = pe.GetMetadataReader();
foreach (var th in md.TypeDefinitions) {
  var t = md.GetTypeDefinition(th);
  var name = md.GetString(t.Namespace) + "." + md.GetString(t.Name);
  if (typeFilter != null && name != typeFilter) continue;
  Console.WriteLine($"TYPE {name} {t.Attributes & TypeAttributes.VisibilityMask} base={(t.BaseType.IsNil ? "" : t.BaseType.Kind == HandleKind.TypeReference ? md.GetString(md.GetTypeReference((TypeReferenceHandle)t.BaseType).Name) : "def")}");
  foreach (var mh in t.GetMethods()) { var m = md.GetMethodDefinition(mh); Console.WriteLine($"  M {md.GetString(m.Name)} {m.Attributes & MethodAttributes.MemberAccessMask} {( (m.Attributes & MethodAttributes.Virtual)!=0 ? "virtual":"")}{((m.Attributes & MethodAttributes.Abstract)!=0?" abstract":"")}"); }
  foreach (var fh in t.GetFields()) { var f = md.GetFieldDefinition(fh); Console.WriteLine($"  F {md.GetString(f.Name)} {f.Attributes & FieldAttributes.FieldAccessMask}"); }
}
