using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS_B_HP.Core
{
    public abstract class OverlayManagerBase : IDisposable
    {
        Dictionary<string, Font> fontsCollection;

        protected Dictionary<string, Font> FontsCollection { get { return fontsCollection; } }

        protected OverlayManagerBase()
        {
            this.fontsCollection = CreateFonts();
        }

        protected abstract Dictionary<string, Font> CreateFonts();

        #region IDisposable implementation
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IEnumerable<string> keysCollection = new List<string>(this.fontsCollection.Keys);
                foreach (string key in keysCollection)
                {
                    if (fontsCollection[key] != null)
                    {
                        this.fontsCollection[key].Dispose();
                        this.fontsCollection[key] = null;
                    }
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~OverlayManagerBase()
        {
            Dispose(false);
        }
        #endregion
    }
}
