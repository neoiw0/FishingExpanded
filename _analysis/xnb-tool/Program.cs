using System.Reflection;
using System.Text;
class P {
    static Stream MakeLzx(Stream input, int decompressedSize, int compressedSize) {
        var asm = Assembly.LoadFrom(@"D:\GGGGG\K1515\MonoGame.Framework.dll");
        var t = asm.GetType("MonoGame.Framework.Utilities.LzxDecoderStream");
        var ctor = t.GetConstructor(new[] { typeof(Stream), typeof(int), typeof(int) });
        return (Stream)ctor.Invoke(new object[] { input, decompressedSize, compressedSize });
    }
    static void Main(string[] args) {
        var b = File.ReadAllBytes(args[0]);
        int decompressedSize = BitConverter.ToInt32(b, 10);
        int compressedSize = b.Length - 14;
        using var ms = new MemoryStream(b, 14, compressedSize);
        using var outMs = new MemoryStream();
        using (var s = MakeLzx(ms, decompressedSize, compressedSize)) { s.CopyTo(outMs); }
        var data = outMs.ToArray();
        foreach (var spec in args[1].Split(',')) {
            var parts = spec.Split(':');
            string id = parts[0], name = parts[1];
            int pos = FindItem(data, id, name);
            if (pos < 0) { Console.WriteLine(name + " NOT FOUND"); continue; }
            // pos 指向 Name 之后。每个 string 字段: typeIdx(7bit) + 7bit len + str
            string display = ReadTypedStr(data, ref pos);
            string desc = ReadTypedStr(data, ref pos);
            string type = ReadTypedStr(data, ref pos);
            int category = BitConverter.ToInt32(data, pos);
            Console.WriteLine($"{name}({id}): type='{type}' category={category}");
        }
    }
    static int FindItem(byte[] data, string id, string name) {
        byte[] idBytes = Encoding.UTF8.GetBytes(id);
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        for (int i = 0; i <= data.Length - 10; i++) {
            if (data[i] != idBytes.Length) continue;
            bool idOk = true;
            for (int j = 0; j < idBytes.Length; j++) if (data[i + 1 + j] != idBytes[j]) { idOk = false; break; }
            if (!idOk) continue;
            int p = i + 1 + idBytes.Length;
            // value typeIdx (7bit, 单字节即可)
            if ((data[p] & 0x80) != 0) continue;
            p++;
            // Name: typeIdx + 7bit len + name
            if ((data[p] & 0x80) != 0) continue;
            p++;
            if (data[p] != nameBytes.Length) continue;
            bool nmOk = true;
            for (int j = 0; j < nameBytes.Length; j++) if (data[p + 1 + j] != nameBytes[j]) { nmOk = false; break; }
            if (!nmOk) continue;
            return p + 1 + nameBytes.Length;
        }
        return -1;
    }
    static string ReadTypedStr(byte[] data, ref int pos) {
        if ((data[pos] & 0x80) != 0) { pos++; } // typeIdx 多字节(理论)
        pos++;
        int len = 0, shift = 0;
        while (true) { byte x = data[pos++]; len |= (x & 0x7F) << shift; shift += 7; if ((x & 0x80) == 0) break; }
        string s = Encoding.UTF8.GetString(data, pos, len);
        pos += len;
        return s;
    }
}
