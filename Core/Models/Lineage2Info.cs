using NC.ToolNet.Net;

namespace PetitionD.Core.Models;

public class Lineage2Info
{
    public const int COORDINATE_LEN = 255;

    public int Race { get; set; }
    public int Class { get; set; }
    public int Level { get; set; }
    public int Disposition { get; set; }
    public int SsPosition { get; set; }
    public int NewChar { get; set; }
    public string Coordinate { get; set; } = string.Empty;

    public void Pack(Packer packer)
    {
        packer.AddInt32(Race);
        packer.AddInt32(Class);
        packer.AddInt32(Level);
        packer.AddInt32(Disposition);
        packer.AddInt32(SsPosition);
        packer.AddInt32(NewChar);
        packer.AddString(Coordinate);
    }

    public void Unpack(Unpacker unpacker)
    {
        Race = unpacker.GetInt32();
        Class = unpacker.GetInt32();
        Level = unpacker.GetInt32();
        Disposition = unpacker.GetInt32();
        SsPosition = unpacker.GetInt32();
        NewChar = unpacker.GetInt32();
        Coordinate = unpacker.GetStringMax(COORDINATE_LEN);
    }
}