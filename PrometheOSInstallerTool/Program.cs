using Mono.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace PrometheOSPacker
{
    public enum Installer
    {
        Custom,
        Ace,
        Andr0,
        Booter,
        Modzville,
        Nemesis
    }

    internal class Program
    {
        private static bool shouldShowHelp = false;
        private static string firmware = string.Empty;
        private static string installer = string.Empty;
        private static string custom = string.Empty;

        private static byte[] GetInstallerLogoData(Image<Argb32> image)
        {
            image.Mutate(i => i.Resize(178, 46));

            var result = new byte[32768];

            result[0] = (byte)'I';
            result[1] = (byte)'M';
            result[2] = 178;
            result[3] = 46;

            var offset = 4;
            for (int y = 0; y < 46; y++)
            {
                for (int x = 0; x < 178; x++)
                {
                    result[offset + 0] = image[x, y].R;
                    result[offset + 1] = image[x, y].G;
                    result[offset + 2] = image[x, y].B;
                    result[offset + 3] = image[x, y].A;
                    offset += 4;
                }
            }

            return result;
        }

        private static void Process(string firmwarePath, Image<Argb32> image)
        {
            var firmware = File.ReadAllBytes(firmwarePath);
            var installerLogoData = GetInstallerLogoData(image);
            Array.Copy(installerLogoData, 0, firmware, 0x1F8000, installerLogoData.Length);
            File.WriteAllBytes(firmwarePath, firmware);
        }

        private static void Main(string[] args)
        {
            var options = new OptionSet {
                { "f|firmware=", "prometheos.bin path to modify.", f => firmware = f },
                { "i|installer=", "Installer logo to embed (Ace, Andr0, Booter, Modzville or Nemesis).", i => installer = i },
                { "c|custom=", "Custom logo image path to embed (178px x 46px).", c => custom = c },
                { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
            };

            try
            {
                List<string> extra = options.Parse(args);

                if (shouldShowHelp || args.Length == 0)
                {
                    Console.WriteLine("PrometheOSInstallerTool: ");
                    options.WriteOptionDescriptions(System.Console.Out);
                    return;
                }

                if (string.IsNullOrEmpty(firmware) == true)
                {
                    throw new OptionException("Firmware path is invalid", "firmware");
                }

                firmware = Path.GetFullPath(firmware);
                if (File.Exists(firmware) == false)
                {
                    throw new OptionException("Firmware file does not exist.", "firmware");
                }

                if (new FileInfo(firmware).Length != 2048 * 1024)
                {
                    throw new OptionException("Firmware file has invalid size.", "firmware");
                }

                if (string.IsNullOrEmpty(installer) == true && string.IsNullOrEmpty(custom) == true)
                {
                    throw new OptionException("Specify Installer or Custom logo.", "installer");
                }

                if (string.IsNullOrEmpty(installer) == false && string.IsNullOrEmpty(custom) == false)
                {
                    throw new OptionException("You can not specify Installer and Custom at same time.", "installer");
                }

                if (string.IsNullOrEmpty(custom) == false)
                {
                    if (string.IsNullOrEmpty(custom) == true)
                    {
                        throw new OptionException("Custom path is invalid", "custom");
                    }

                    custom = Path.GetFullPath(custom);
                    if (File.Exists(custom) == false)
                    {
                        throw new OptionException("Custom file does not exist.", "custom");
                    }
                }
                
                if (string.IsNullOrEmpty(installer) == false)
                {
                    string[] installers = Enum.GetNames(typeof(Installer));
                    if (installers.Any(i => i.Equals(installer, StringComparison.OrdinalIgnoreCase)) == false)
                    {
                        throw new OptionException("Invalid Installer specified.", "installer");
                    }
                    var imageFileData = ResourceLoader.GetEmbeddedResourceBytes($"PrometheOSInstallerTool.Resources.{installer}.png");
                    using var image = Image.Load<Argb32>(imageFileData);
                    Process(firmware, image);
                }
                else
                {
                    using var image = Image.Load<Argb32>(custom);
                    Process(firmware, image);
                }

                Console.WriteLine("Done.");
            }
            catch (OptionException e)
            {
                Console.Write("PrometheOSInstallerTool: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `PrometheOSInstallerTool --help' for more information.");
                return;
            }
        }
    }
}