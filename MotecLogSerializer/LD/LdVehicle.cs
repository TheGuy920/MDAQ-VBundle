using System.Text;

namespace MotecLogSerializer.LdParser;

public class LdVehicle
{
    public string Id { get; set; }
    public uint Weight { get; set; }
    public string Type { get; set; }
    public string Comment { get; set; }

    public static LdVehicle FromFile(BinaryReader reader)
    {
        var id = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        reader.BaseStream.Seek(128, SeekOrigin.Current); // Skip 128 bytes
        var weight = reader.ReadUInt32();
        var type = Encoding.ASCII.GetString(reader.ReadBytes(32)).TrimEnd('\0');
        var comment = Encoding.ASCII.GetString(reader.ReadBytes(32)).TrimEnd('\0');

        return new LdVehicle
        {
            Id = id,
            Weight = weight,
            Type = type,
            Comment = comment
        };
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Encoding.ASCII.GetBytes(Id.PadRight(64, '\0')));
        writer.Write(new byte[128]); // Write 128 zero bytes
        writer.Write(Weight);
        writer.Write(Encoding.ASCII.GetBytes(Type.PadRight(32, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(Comment.PadRight(32, '\0')));
    }

    public override string ToString() => $"{Id} (type: {Type}, weight: {Weight}, {Comment})";
}

