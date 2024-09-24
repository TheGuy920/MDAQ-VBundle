using System.Text;

namespace MotecLogSerializer.LdParser;

public static class LdParser
{
    public static List<LdChan> ReadChannels(string filePath, uint metaPtr)
    {
        var channels = new List<LdChan>();
        while (metaPtr != 0)
        {
            var channel = LdChan.FromFile(filePath, metaPtr);
            channels.Add(channel);
            metaPtr = channel.NextMetaPtr;
        }
        return channels;
    }

    public static (LdHead, List<LdChan>) ReadLdFile(string filePath)
    {
        var head = LdHead.FromFile(new FileStream(filePath, FileMode.Open, FileAccess.Read));
        var channels = ReadChannels(filePath, head.MetaPtr);
        return (head, channels);
    }

    private static string DecodeString(byte[] bytes)
    {
        try
        {
            return Encoding.ASCII.GetString(bytes).Trim().TrimEnd('\0').Trim();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Could not decode string: {e.Message} - {BitConverter.ToString(bytes)}");
            return string.Empty;
        }
    }
}

