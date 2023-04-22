using Parser;
using System.Text;

var path = @"../../../g2nk_ou.bin";

var reader = new Reader(path, Encoding.GetEncoding(1250));
var list = reader.Parse();

Console.ReadLine();