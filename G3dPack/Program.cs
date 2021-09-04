using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace G3dPack
{
    class Program
    {
        static bool CIflag = false;
        static int numTex = 0;
        static int[] textureInfo;
        static string texturetxtfilename;
        static string meshtxtfilename;
        static void Main(string[] args)
        {

            Console.WriteLine("Welcome to Make G3D\n");

            Console.WriteLine("Checking mesh count");
            try
            {
                var meshcount = Directory.GetFiles("meshes\\", "*.l3d").Length;
                if (meshcount >= 704)// Creature Isle has 704 by default
                {
                    Console.WriteLine("Packing for Creature Isle");
                    CIflag = true;
                    texturetxtfilename = "creatureisletextures.txt";
                    meshtxtfilename = "creatureislemesh.txt";
                }
                else if (meshcount >= 626) // Standard Black & White has 626 by default
                {
                    Console.WriteLine("Packing for standard Black & White");
                    texturetxtfilename = "standardtextures.txt";
                    meshtxtfilename = "standardmesh.txt";
                }
                else 
                {
                    Console.WriteLine("Error: not enough meshes, need minimum of 626");
                    return;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error accessing mesh folder");
                return;
            }

            Console.WriteLine("[Linking High res textures]");
            //Straight after the file header the next 4 bytes contain the texture number. ..
            //therefore we retrieve the number of hi-res textures.
            try
            {
                using (var fileIn = File.OpenText(texturetxtfilename))
                {

                    numTex = int.Parse(fileIn.ReadLine());
                    textureInfo = new int[numTex];
                    Console.WriteLine("there are: " + numTex + " textures");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not open file: '"+texturetxtfilename+"'\n..closing");
                return; //Without the information the program will crash later on, so we'll close it now instead.
            }

            //Now we open the output file
            using (var fileOut = new BinaryWriter(File.OpenWrite("AllMeshes.g3d")))
            {
                Console.WriteLine("file opened successfully");
                //Now write the header
                fileOut.Write(new[] { 'L', 'i', 'O', 'n', 'H', 'e', 'A', 'd' });
                HighRes(fileOut);
                //high res texture linking is complete

                if (CIflag)
                {
                    LowRes(fileOut);
                    Meshes(fileOut);
                }
                else
                {
                    Meshes( fileOut);
                    LowRes( fileOut);
                }


            }
        }

        private static void LowRes(BinaryWriter fileOut)
        {
            for (int texLoop = numTex; texLoop > 0; texLoop--)
            {

                //next, we need to output the low res textures
                char[] lowTexHeader = { 'L', 'O', 'W' };
                fileOut.Write(lowTexHeader);

                //write the number of textures
                var temp = texLoop.ToString("x");
                fileOut.Write(temp.ToCharArray());

                //white space
                fileOut.Write(new byte[29 - temp.Length]);

                //Now read in the sequence of dds textures
                try
                {
                    using (var fileIn = new BinaryReader(File.OpenRead(String.Format("textures\\{0}-low.dds", texLoop))))
                    {


                        //Find the length of the input file
                        int fileEof = (int)fileIn.BaseStream.Length;
                        fileEof += 12;
                        //Setup and output texture length including the header section of 12 bytes
                        fileOut.Write(fileEof);
                        if (CIflag)
                        {
                            fileOut.Write(fileEof);
                            fileOut.Write(texLoop);
                            fileOut.Write(textureInfo[texLoop - 1]);
                        }
                        else
                        {
                            //more white space
                            fileOut.Write(new byte[12]);
                        }

                        fileEof -= 12;
                        fileOut.Write(fileEof);

                        fileIn.BaseStream.Seek(4, SeekOrigin.Begin);
                        fileOut.Write(fileIn.ReadBytes(fileEof));

                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not open texture ID: " + texLoop + "\n\nclosing...");
                    return;
                }
            }
        }

        private static void HighRes(BinaryWriter fileOut)
        {
            //Enter a texture loop
            for (int texLoop = numTex; texLoop > 0; texLoop--)
            {
                //Write the texture number

                // output the texture number
                var temp = texLoop.ToString("x");
                fileOut.Write(temp.ToCharArray());

                //28 useless bytes go here, dont know why.
                fileOut.Write(new byte[32 - temp.Length]);

                //all other workings now need to be done after the input texture is opened
                //Maybe the otehrs too - lcean up!

                //Now read in the sequence of dds textures
                try
                {
                    using (var fileIn = new BinaryReader(File.OpenRead(String.Format("textures\\{0}.dds", texLoop))))
                    {

                        //Find the length of the input file
                        int fileEof = (int)fileIn.BaseStream.Length;
                        fileEof += 12;
                        //Setup and output texture length including the header section of 12 bytes
                        fileOut.Write(fileEof);
                        fileOut.Write(fileEof);

                        fileOut.Write(texLoop);

                        //Get and print texture type to file, also store type to an array for writing
                        //in the info section

                        fileIn.BaseStream.Seek(84, SeekOrigin.Begin);

                        // Need to find out if the texture file is saved as DXT1 (type 1) or DXT3 (type 2)
                        if (fileIn.ReadString(4).Equals("DXT3"))
                        {
                            textureInfo[texLoop - 1] = 2;
                        }
                        else
                        {
                            textureInfo[texLoop - 1] = 1;
                        }

                        //long texType;
                        fileOut.Write(textureInfo[texLoop - 1]);

                        fileEof -= 12;
                        fileOut.Write(fileEof);

                        fileEof -= 4;

                        // need to check to see whether the dds file is 0 bytes

                        fileIn.BaseStream.Seek(4, SeekOrigin.Begin);

                        fileOut.Write(fileIn.ReadBytes(fileEof));

                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Could not open texture ID: " + texLoop + "\n\nclosing...");
                    return;
                }

            }

            Console.WriteLine("High res texture linking completed");
        }

        private static void Meshes(BinaryWriter fileOut)
        {

            //First comes the info header section
            char[] infoHeader = { 'I', 'N', 'F', 'O' };
            fileOut.Write(infoHeader);

            //Just fill this with white space
            fileOut.Write(new byte[28]);

            //Length of INFO Section
            int spaceLeft = 4100;
            fileOut.Write(spaceLeft); // 4100 bytes in original

            //before the actual texture info we write the number of textures
            fileOut.Write(numTex);

            spaceLeft -= 4;
            for (int texLoop = numTex; texLoop > 0; texLoop--)
            {

                fileOut.Write(texLoop);
                fileOut.Write(textureInfo[texLoop - 1]);

                spaceLeft -= 8;
            }
            if (CIflag)
            {
                var temp = new byte[spaceLeft];
                for (int i = 0; i < spaceLeft; i++)
                {
                    temp[i] = 0xCD;
                }
                fileOut.Write(temp);
            }
            else
            {

                //More white space

                fileOut.Write(new byte[148]);

                //quite a big heafer section here. Dont know what any of it does exactly though.
                byte[] unknownHeader = { 132, 115, 98, 81, 00, 00, 00, 00, 39, 03, 131, 0, 1, 16, 0, 0, 120, 1, 194, 00, 120, 1, 194, 00 };

                fileOut.Write(unknownHeader);
                spaceLeft -= 172;

                //now some more empty bytes.
                fileOut.Write(new byte[spaceLeft]);
            }

            /////////////////////////////////////////////////
            // Mesh header section then the mesh data
            /////////////////////////////////////////////////

            char[] meshHeader = { 'M', 'E', 'S', 'H', 'E', 'S' };
            fileOut.Write(meshHeader);

            // yet more bytes of nothing in particular
            fileOut.Write(new byte[26]);

            //Length of mesh section
            if (CIflag)
                fileOut.Write(0x7BFDC2);
            else
                fileOut.Write(0x6A351A);

            char[] smallMeshHeader = { 'M', 'K', 'J', 'C' };
            fileOut.Write(smallMeshHeader);

            string strNumMesh;
            //		char strOffset[255];
            int numberOfMeshes;
            int[] meshOffsets;
            string meshName;
            int sumOffset = 0;

            //Now we work out the offsest, first open the mesh list
            try
            {
                using (var fileIn = File.OpenText(meshtxtfilename))
                {
                    strNumMesh = fileIn.ReadLine();
                    numberOfMeshes = int.Parse(strNumMesh);

                    meshOffsets = new int[numberOfMeshes];
                    //meshName = new char[numberOfMeshes];
                    fileOut.Write(numberOfMeshes);

                    // The first mesh offset is the size of the offset data block.
                    meshOffsets[numberOfMeshes - 1] = ((numberOfMeshes * 4) + 8);
                    fileOut.Write(meshOffsets[numberOfMeshes - 1]);
                    sumOffset = meshOffsets[numberOfMeshes - 1];

                    for (long meshNum = (numberOfMeshes - 1); meshNum > 0; meshNum--)
                    {

                        meshName = fileIn.ReadLine();

                        try
                        {



                            using (var fileMesh = File.OpenRead(string.Format("meshes\\{0}.l3d", meshName)))
                            {

                                meshOffsets[meshNum - 1] = ((int)fileMesh.Length + meshOffsets[meshNum]);
                                //sumOffset += meshOffsets[meshNum];

                                fileOut.Write(meshOffsets[meshNum - 1]);

                            }
                        }

                        catch (Exception)
                        {
                            Console.WriteLine("Cannot open mesh file:" + meshName + ".l3d");
                            return;
                        }

                    }


                }
            }
            catch (Exception)

            {
                Console.WriteLine("Could not open file: '"+ meshtxtfilename + "'\n..closing");
                return; //Without the information the program will crash later on, so we'll close it now instead.
            }


            //Straight after the offsets are printed, we print the actual mesh info.
            try
            {
                using (var fileIn = File.OpenText(meshtxtfilename))
                {
                    fileIn.ReadLine();

                    for (int meshNum = numberOfMeshes; meshNum > 0; meshNum--)
                    {

                        meshName = fileIn.ReadLine();
                        try
                        {
                            fileOut.Write(File.ReadAllBytes(string.Format("meshes\\{0}.l3d", meshName)));
                        }
                        catch (Exception)
                        {

                            Console.WriteLine("Cannot open mesh file:" + meshName + ".l3d");
                            return;
                        }

                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Could not open "+ meshtxtfilename + "...\nquitting program.");
                return;
            }
        }
    }

    static class Utils
    {
        public static string ReadString(this BinaryReader br, int count)
        {
            return new string(br.ReadChars(count));
        }
    }
}
