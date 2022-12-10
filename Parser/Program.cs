using Parser;

var path = @"../../../g2nk_ou.bin";

var reader = new Reader(path, 1250);
var list = reader.Parse();

Console.ReadLine();