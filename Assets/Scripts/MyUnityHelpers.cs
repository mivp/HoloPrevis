using UnityEngine;
using System;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

static class MyUnityHelpers
{
    static public void ExtractZipFile(string zipFilePath, string outDirPath)
    {
        //Debug.Log("ExtractZipFile " + zipFilePath);
        if (System.IO.File.Exists(zipFilePath) == false)
            return;

        using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath)))
        {
            ZipEntry theEntry;
            while ((theEntry = s.GetNextEntry()) != null)
            {

                string directoryName = Path.GetDirectoryName(theEntry.Name);
                string fileName = Path.GetFileName(theEntry.Name);

                string fullDirPath = Path.Combine(outDirPath, directoryName);
                string fullFilePath = Path.Combine(outDirPath, theEntry.Name);

                //Debug.Log(theEntry.Name + " " + directoryName + " " + fileName);
                // create directory
                if (directoryName.Length > 0)
                {
                    try
                    {
                        Directory.CreateDirectory(fullDirPath);
                    }
                    catch
                    {
                        Debug.Log("Cannot create folder: " + fullDirPath);
                    }
                }
                
                if (fileName != String.Empty)
                {
                    try
                    {
                        using (FileStream streamWriter = File.Create(fullFilePath))
                        {
                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0)
                                {
                                    streamWriter.Write(data, 0, size);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // opps, something went wrong
                        Debug.Log("Cannot open file to write: " + fullFilePath);
                    }
                }
                
            }
        }
    }

    static public string GetTextFileContent(string filename)
    {
        StreamReader reader = new StreamReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        string text = reader.ReadToEnd();
        reader.Dispose();
        return text;
        /*
        byte[] bytes = UnityEngine.Windows.File.ReadAllBytes(filename);
        string fileData = System.Text.Encoding.ASCII.GetString(bytes);
        return fileData;
        */
    }

    static public Bounds GetGameObjectBound(GameObject g)
    {
        var b = new Bounds(g.transform.position, Vector3.zero);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

    static public void UpdateObjectTransform(GameObject gameObject, Vector3 position, Vector3 scale)
    {
        gameObject.transform.localPosition = position;
        gameObject.transform.localScale = scale;
        foreach (Transform child in gameObject.transform)
        {
            GameObject c = child.gameObject;
            c.transform.localPosition = Vector3.zero;
        }
    }

    static public Texture3D Build3DTextureFromXRW(string xrwpath)
    {
        // read header
        int nx, ny, nz;
        float wdx, wdy, wdz;
        int offset = 0;
        Texture3D tex = null;

        using (var stream = new FileStream(xrwpath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            byte[] b4 = new byte[4];
            stream.Read(b4, offset, 4);
            nx = System.BitConverter.ToInt32(b4, 0);
            
            stream.Read(b4, offset, 4);
            ny = System.BitConverter.ToInt32(b4, 0);
            
            stream.Read(b4, offset, 4);
            nz = System.BitConverter.ToInt32(b4, 0);
            
            byte[] b8 = new byte[8];
            stream.Read(b8, offset, 8);
            wdx = System.BitConverter.ToSingle(b8, 0);
            
            stream.Read(b8, offset, 8);
            wdy = System.BitConverter.ToSingle(b8, 0);
            
            stream.Read(b8, offset, 8);
            wdz = System.BitConverter.ToSingle(b8, 0);
            
            tex = new Texture3D(nx, ny, nz, TextureFormat.ARGB32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 0;
            var max = nx * ny * nz;

            int i = 0;
            Color[] colors = new Color[max];
            float inv = 1f / 255.0f;
            for (i = 0; i < max; i++)
            {
                int v = stream.ReadByte();
                float f = v * inv;
                colors[i] = new Color(f, f, f, f);
            }
            tex.SetPixels(colors);
            tex.Apply();
        }

        return tex;
    }

    static Color GetColorInRamp(float value, float minVal, float maxVal)
    {
        float[,] c = new float[,] {
            {0F, 0, 0, 0, 0},
            {0.023438F, 60, 60, 60, 1},
            {0.046875F, 18, 15, 0, 1},
            {0.066641F, 248, 144, 87, 0.38F},
            {0.103047F, 252, 224, 166, 1},
            {0.146016F, 255, 81, 0, 1},
            {0.200703F, 72, 0, 22, 1},
            {0.236084F, 246, 245, 122, 1},
            {0.310078F, 255, 0, 0, 1},
            {0.355F, 255, 255, 255, 0},
            {0.894062F, 255, 255, 255, 0},
            {1F, 255, 255, 255, 1}
        };
        float percent = (value - minVal) / (maxVal - minVal);
        if (percent == 0) return new Color(c[0, 1] / 255, c[0, 2] / 255, c[0, 3] / 255, c[0, 4]);
        if (percent == 1) return new Color(c[11, 1] / 255, c[11, 2] / 255, c[11, 3] / 255, c[11, 4]);

        Vector2 colorRange = new Vector2();
        for (int i = 0; i < 12; i++)
        {
            if (percent <= c[i, 0])
            {
                colorRange[0] = i - 1;
                colorRange[1] = i;
                break;
            }
        }

        int ind0 = (int)colorRange[0];
        int ind1 = (int)colorRange[1];

        float ratio = (percent - c[ind0, 0]) / (c[ind1, 0] - c[ind0, 0]);
        return new Color(
            ratio * c[ind0, 1] / 255 + (1 - ratio) * c[ind1, 1] / 255,
            ratio * c[ind0, 2] / 255 + (1 - ratio) * c[ind1, 2] / 255,
            ratio * c[ind0, 3] / 255 + (1 - ratio) * c[ind1, 3] / 255,
            ratio * c[ind0, 4] + (1 - ratio) * c[ind1, 1]
        );
    }


    static public Texture2D Build2DTransferFunction(string jsonPath)
    {
        var texture = new Texture2D(256, 1, TextureFormat.ARGB32, false);
        //texture.alphaIsTransparency = true;
        for (int i = 0; i < 256; i++)
        {
            Color color = GetColorInRamp((float)i / 256, 0, 1);
            texture.SetPixel(i, 1, color);
        }
        texture.Apply();
        return texture;
    }

}
