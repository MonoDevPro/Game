using System;

namespace Game.Domain.Entities;

// Helpers opcionais para interoperar com Tile[,,] quando necessário
public static class Map3DConversions
{
    // Converte o armazenamento 1D do Map para um array 3D [x,y,z] (cópia)
    public static Tile[,,] ToArray3D(this Map map)
    {
        var arr = new Tile[map.Width, map.Height, map.Layers];
        int layerSize = map.Width * map.Height;

        for (int z = 0; z < map.Layers; z++)
        {
            int offset = z * layerSize;
            int i = 0;
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++, i++)
                arr[x, y, z] = map.Tiles[offset + i];
        }
        return arr;
    }

    // Cria um Map a partir de um array 3D [x,y,z] (cópia)
    public static Map FromArray3D(string name, Tile[,,] source, bool borderBlocked = false)
    {
        int w = source.GetLength(0);
        int h = source.GetLength(1);
        int l = source.GetLength(2);

        var map = new Map(name, w, h, l, borderBlocked);
        int layerSize = w * h;

        for (int z = 0; z < l; z++)
        {
            int offset = z * layerSize;
            int i = 0;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++, i++)
                map.Tiles[offset + i] = source[x, y, z];
        }
        return map;
    }
}