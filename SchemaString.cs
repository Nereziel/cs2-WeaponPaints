using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.CompilerServices;
using System.Text;

namespace WeaponPaints;

public class SchemaString<TSchemaClass> : NativeObject where TSchemaClass : NativeObject
{
	internal SchemaString(TSchemaClass instance, string member) : base(Schema.GetSchemaValue<nint>(instance.Handle, typeof(TSchemaClass).Name!, member))
	{ }

	internal unsafe void Set(string str)
	{
		var bytes = Encoding.UTF8.GetBytes(str);

		for (var i = 0; i < bytes.Length; i++)
		{
			Unsafe.Write((void*)(Handle.ToInt64() + i), bytes[i]);
		}

		Unsafe.Write((void*)(Handle.ToInt64() + bytes.Length), 0);
	}
}