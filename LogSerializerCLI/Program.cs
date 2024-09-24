// See https://aka.ms/new-console-template for more information
using MotecLogSerializer.LdParser;
var data = LdData.FromFile("D:\\S1_#5264_20240921_135809.ld");

Console.WriteLine($"Head: {data.Head}");
Console.WriteLine($"Channels: {data.Channels.Count}");
