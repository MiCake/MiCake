using System.Collections;
using System.Reflection;

namespace MiCake.Core.Modularity
{
    public class MiCakeModuleCollection : IMiCakeModuleCollection
    {
        private readonly List<MiCakeModuleDescriptor> _descriptors = new();

        public MiCakeModuleDescriptor this[int index]
        {
            get
            {
                return _descriptors[index];
            }
            set
            {
                _descriptors[index] = value;
            }
        }

        public int Count => _descriptors.Count;

        public bool IsReadOnly => false;

        public void Add(MiCakeModuleDescriptor item)
        {
            _descriptors.Add(item);
        }

        public void Clear()
        {
            _descriptors.Clear();
        }

        public bool Contains(MiCakeModuleDescriptor item)
        {
            return _descriptors.Contains(item);
        }

        public void CopyTo(MiCakeModuleDescriptor[] array, int arrayIndex)
        {
            _descriptors.CopyTo(array, arrayIndex);
        }

        public Assembly[] GetAssemblies(bool includeCoreModules = true)
        {
            if (includeCoreModules)
            {
                return _descriptors.Select(x => x.Assembly).Distinct().ToArray();
            }
            else
            {
                return _descriptors.Where(x => !x.IsCoreModule).Select(x => x.Assembly).Distinct().ToArray();
            }
        }

        public IEnumerator<MiCakeModuleDescriptor> GetEnumerator()
        {
            return _descriptors.GetEnumerator();
        }

        public int IndexOf(MiCakeModuleDescriptor item)
        {
            return _descriptors.IndexOf(item);
        }

        public void Insert(int index, MiCakeModuleDescriptor item)
        {
            _descriptors.Insert(index, item);
        }

        public bool Remove(MiCakeModuleDescriptor item)
        {
            return _descriptors.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _descriptors.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
