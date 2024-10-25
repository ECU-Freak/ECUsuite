using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECUsuite.ECU.Base
{
    public class SymbolFileHandler
    {

        /// <summary>
        /// save the data to a file in binary format
        /// </summary>
        /// <param name="path"></param>
        public void save(string path, Symbols data)
        {
            var serialData = MemoryPackSerializer.Serialize(data);

            File.WriteAllBytes(path, serialData);

        }

        /// <summary>
        /// laod a binary file
        /// </summary>
        /// <param name="path"></param>
        public Symbols load(string path)
        {
            Symbols retval = new Symbols();
            retval = MemoryPackSerializer.Deserialize<Symbols>(File.ReadAllBytes(path));

            return retval;
        }
    }
}
