using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using ApUtilitiesLib;

namespace ApplicationLib
{
    public class Application : IExtensionApplication
    {
        public void Initialize()
        {
            MessageBox.Show("Плагин пикетов успешно загружен!" +
                "\nДля вывода информации о трассе ввидите: ATInfo" +
                "\nДля рассчета пикетов со смещением: ATStation");
        }

        public void Terminate() { }

        
        [CommandMethod("ATInfo")]
        public void AtInfo()
        {
            ApUtilities.AtInfo();
        }

        [CommandMethod("ATStation")]
        public void AtStation()
        {
            ApUtilities.AtStation();
        }
    }
}