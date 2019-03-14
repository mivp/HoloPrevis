using UnityEngine;
using System;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

class MyUnityHelpers
{
    public void ExtractZipFile(string zipFilePath, string outDirPath)
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
}
