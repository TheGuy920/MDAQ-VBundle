using System.Text;

namespace MotecLogSerializer.LdParser;

public class LdHead
{
    public uint MetaPtr { get; set; }
    public uint DataPtr { get; set; }
    public uint EventPtr { get; set; }
    public LdEvent Event { get; set; }
    public string Driver { get; set; }
    public string VehicleId { get; set; }
    public string Venue { get; set; }
    public DateTime DateTime { get; set; }
    public string ShortComment { get; set; }

    public static LdHead FromFile(FileStream fileStream)
    {
        var reader = new BinaryReader(fileStream);
        reader.BaseStream.Seek(8, SeekOrigin.Current); // Skip 8 bytes
        var metaPtr = reader.ReadUInt32();
        var dataPtr = reader.ReadUInt32();
        reader.BaseStream.Seek(20, SeekOrigin.Current); // Skip 20 bytes
        var eventPtr = reader.ReadUInt32();
        reader.BaseStream.Seek(24, SeekOrigin.Current); // Skip 24 bytes
        reader.BaseStream.Seek(10, SeekOrigin.Current); // Skip 10 bytes
        reader.BaseStream.Seek(20, SeekOrigin.Current); // Skip 20 bytes
        var date = Encoding.ASCII.GetString(reader.ReadBytes(16)).TrimEnd('\0');
        reader.BaseStream.Seek(16, SeekOrigin.Current); // Skip 16 bytes
        var time = Encoding.ASCII.GetString(reader.ReadBytes(16)).TrimEnd('\0');
        reader.BaseStream.Seek(16, SeekOrigin.Current); // Skip 16 bytes
        var driver = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        var vehicleId = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        reader.BaseStream.Seek(64, SeekOrigin.Current); // Skip 64 bytes
        var venue = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');
        reader.BaseStream.Seek(1094, SeekOrigin.Current); // Skip 1094 bytes
        var shortComment = Encoding.ASCII.GetString(reader.ReadBytes(64)).TrimEnd('\0');

        var ldHead = new LdHead
        {
            MetaPtr = metaPtr,
            DataPtr = dataPtr,
            EventPtr = eventPtr,
            Driver = driver,
            VehicleId = vehicleId,
            Venue = venue,
            DateTime = ParseDateTime(date, time),
            ShortComment = shortComment
        };

        if (eventPtr > 0)
        {
            reader.BaseStream.Seek(eventPtr, SeekOrigin.Begin);
            ldHead.Event = LdEvent.FromFile(reader);
        }

        return ldHead;
    }

    public void Write(BinaryWriter writer, int channelCount)
    {
        writer.Write(0x40);
        writer.Write(new byte[4]); // Write 4 zero bytes
        writer.Write(MetaPtr);
        writer.Write(DataPtr);
        writer.Write(new byte[20]); // Write 20 zero bytes
        writer.Write(EventPtr);
        writer.Write(new byte[24]); // Write 24 zero bytes
        writer.Write((ushort)1);
        writer.Write((ushort)0x4240);
        writer.Write((ushort)0xf);
        writer.Write(0x1f44);
        writer.Write(Encoding.ASCII.GetBytes("ADL".PadRight(8, '\0')));
        writer.Write((ushort)420);
        writer.Write((ushort)0xadb0);
        writer.Write(channelCount);
        writer.Write(new byte[4]); // Write 4 zero bytes
        writer.Write(Encoding.ASCII.GetBytes(DateTime.ToString("dd/MM/yyyy").PadRight(16, '\0')));
        writer.Write(new byte[16]); // Write 16 zero bytes
        writer.Write(Encoding.ASCII.GetBytes(DateTime.ToString("HH:mm:ss").PadRight(16, '\0')));
        writer.Write(new byte[16]); // Write 16 zero bytes
        writer.Write(Encoding.ASCII.GetBytes(Driver.PadRight(64, '\0')));
        writer.Write(Encoding.ASCII.GetBytes(VehicleId.PadRight(64, '\0')));
        writer.Write(new byte[64]); // Write 64 zero bytes
        writer.Write(Encoding.ASCII.GetBytes(Venue.PadRight(64, '\0')));
        writer.Write(new byte[1024]); // Write 1024 zero bytes
        writer.Write(0xc81a4);
        writer.Write(new byte[66]); // Write 66 zero bytes
        writer.Write(Encoding.ASCII.GetBytes(ShortComment.PadRight(64, '\0')));
        writer.Write(new byte[126]); // Write 126 zero bytes

        if (EventPtr > 0)
        {
            writer.Seek((int)EventPtr, SeekOrigin.Begin);
            Event.Write(writer);
        }
    }

    private static DateTime ParseDateTime(string date, string time)
        => DateTime.ParseExact($"{date} {time}", "dd/MM/yyyy HH:mm:ss", null);



    public override string ToString() =>
        $"driver: {Driver}\n" +
        $"vehicleid: {VehicleId}\n" +
        $"venue: {Venue}\n" +
        $"event: {Event?.Name}\n" +
        $"session: {Event?.Session}\n" +
        $"short_comment: {ShortComment}";
}

