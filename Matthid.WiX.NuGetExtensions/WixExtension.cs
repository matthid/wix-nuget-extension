using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Tools.WindowsInstallerXml;

namespace Matthid.WiX.NuGetExtensions
{
    public class NuGetWixExtension : WixExtension
    {
        private NuGetPreprocessorExtension _extension;

        public override PreprocessorExtension PreprocessorExtension
        {
            get
            {
                if (_extension == null)
                {
                    _extension = new NuGetPreprocessorExtension();
                }

                return _extension;
            }
        }
    }
}
