using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace G3dExtr
{
    class Program
	{
		//file variables
		static int currentTexture;
		static int textureTotal = 0;
		static int fileSize;
		static byte[] fileData;
		static byte[] ddsHeader = Encoding.UTF8.GetBytes("DDS "); //in the g3d file these 4 bytes indicate the filesize, however in a DDS file the first 4 bytes need to be "DDS ".

		static BinaryReader fileIn;
		static void Main(string[] args)
        {
            //make directorys to save textures and meshes too 
            Directory.CreateDirectory("textures");
            Directory.CreateDirectory("meshes");



            try
            {
                fileIn = new BinaryReader(File.OpenRead("AllMeshes.g3d"));
            }
            catch (Exception)
            {
                Console.WriteLine("could not open G3D file");
                return;
            }
            if (new string(fileIn.ReadChars(8)).Equals("LiOnHeAd"))
            {
                fileIn.BaseStream.Seek(40, SeekOrigin.Current); //skip past rest of the file header info

                
                //===============
                // High-res textures are first so we extract them
                //===============
                ExtractHighResTextures();
                fileIn.BaseStream.Seek(-40, SeekOrigin.Current);

                

                if (fileIn.ReadString(4).Equals("INFO")) // Standard Black & White
                {

                    //===============
                    // Mesh offsets come now, then the actual l3d data.
                    //===============


                    fileIn.BaseStream.Seek(4172, SeekOrigin.Current);
                    ExtractMeshes("standardmesh.txt");
                    //		currentOffset += (l3dLength + l3dLength);
                    //		fileIn.Seek(currentOffset, ios::beg);

                    //===============
                    // Before finishing, extract the low res textures
                    //===============
                    ExtractLowResTextures();
                }
                else // Creature Isles
                {
                    fileIn.BaseStream.Seek(-4, SeekOrigin.Current);

                    //===============
                    // Low-res textures come now.
                    //===============

                    ExtractLowResTextures();

                    //===============
                    // Before finishing, extract the mesh offsets then the actual l3d data
                    //===============
                    fileIn.BaseStream.Seek(4176, SeekOrigin.Current);
                    ExtractMeshes("creatureislemesh.txt");

                }

                Console.WriteLine("The contents of the G3D file have been successfully extracted to the \\meshes and \\textures folders.\n");
            }
            Console.WriteLine("Invalid G3D file");
            // clean up
            fileIn.Dispose();
        }

        private static void ExtractLowResTextures()
        {
            Console.WriteLine("Extracting low resolution ('-low') textures..");


            //fileIn.seekg (3, SeekOrigin.Current);
            string lowTexTag = "LOW";
            currentTexture = textureTotal;
            do
            {

                var test = string.Concat(fileIn.ReadChars(3));
                if (!lowTexTag.Equals(test)) // find out the current texture number
                {
                    Console.WriteLine("invalid low textures");
                    return;
                }


                fileIn.BaseStream.Seek(45, SeekOrigin.Current);
                fileSize = fileIn.ReadInt32(); //read in the texture size

                //fileData = new char[fileSize + 1]; //dimension memory to hold the texture data
                fileData = fileIn.ReadBytes(fileSize - 4);

                // ===== Find output dir/ name =====
                string textureFile = string.Format("textures\\{0}-low.dds", currentTexture);

                using (FileStream fileOut = File.OpenWrite(textureFile))
                {
                    fileOut.Write(ddsHeader, 0, 4);
                    fileOut.Write(fileData, 0, fileSize - 4);

                }

                currentTexture--;
                //currentOffset -= 4;

            } while (currentTexture != 0);

            Console.WriteLine("Completed.\n");
        }

        private static void ExtractMeshes(string meshnamefile)
        {
            Console.WriteLine("Extracting l3d meshes");

            int numMeshes;
            int[] meshOffset;
            long meshInfoOffset;
            string l3dHeader = "L3D0";
            int l3dLength;

            numMeshes = fileIn.ReadInt32(); //read in the texture size
            meshInfoOffset = fileIn.BaseStream.Position;
            meshInfoOffset -= 8;
            //numMeshes = 704;

            meshOffset = new int[numMeshes]; //dimension memory to hold mesh offsets

            for (int meshRun = 0; meshRun < numMeshes; meshRun++)
            {
                meshOffset[meshRun] = fileIn.ReadInt32(); //read in the texture size
            }

            using (StreamReader fileHeader = File.OpenText(meshnamefile))
            {

                string meshName;
                int mshCount = 0;
                string meshNumber;

                meshNumber = fileHeader.ReadLine();

                //now read in the actual l3d data
                for (long meshRun = 0; (meshRun) < numMeshes; meshRun++)
                {

                    //dps tuff for file header name
                    mshCount++;
                    meshName = fileHeader.ReadLine();
                    //fileHeader >> meshName;
                    //cout << "mesh" << mshCount << ": " << meshName << "\n";

                    long currentOffset = meshInfoOffset + meshOffset[meshRun];
                    var test = string.Concat(fileIn.ReadChars(4));
                    if (!l3dHeader.Equals(test))
                    {
                        Console.WriteLine("ERROR! Could not find l3d header information.");
                        return;
                    }

                    //fileIn.Seek(currentOffset, ios::beg);
                    fileIn.BaseStream.Seek(4, SeekOrigin.Current); //move forward 4 bytes to get l3d size.
                    l3dLength = fileIn.ReadInt32();
                    //fileData = new char[l3dLength + 1]; //dimension memory to hold the l3d data
                    fileIn.BaseStream.Seek(currentOffset, SeekOrigin.Begin); //now we return to the beginning of the l3d file (move back 8 bytes).
                    fileData = fileIn.ReadBytes(l3dLength);

                    // ===== Find output dir/ name =====
                    string l3dFile = string.Format("meshes\\{0}.l3d", meshName);

                    using (FileStream fileOut = File.OpenWrite(l3dFile))
                    {
                        fileOut.Write(fileData, 0, l3dLength);

                    }

                }
            }
            Console.WriteLine("Completed\n");
        }

        private static void ExtractHighResTextures()
        {
            Console.WriteLine("Extracting high resolution textures..");

            do
            {
                currentTexture = fileIn.ReadInt32(); // find out the current texture number

                if (textureTotal < currentTexture)
                    textureTotal = currentTexture;

                fileIn.BaseStream.Seek(4, SeekOrigin.Current);
                fileSize = fileIn.ReadInt32(); //read in the texture size
                                               //			fileSize -=4;

                //fileData = new char[fileSize + 1]; //dimension memory to hold the texture data

                fileData = fileIn.ReadBytes(fileSize - 4);

                // ===== Find output dir/ name =====

                string textureFile = string.Format("textures\\{0}.dds", currentTexture);

                using (FileStream fileOut = File.OpenWrite(textureFile))
                {
                    fileOut.Write(ddsHeader, 0, 4);
                    fileOut.Write(fileData, 0, fileSize - 4);

                }
                fileIn.BaseStream.Seek(40, SeekOrigin.Current);

            } while (currentTexture != 1);

            Console.WriteLine("Completed\n");
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
