using System.Reactive.Subjects;

namespace BitcoinPKArrayScrambleWorker
{
    public class ScrambleService : IScrambleService
    {
        public Subject<byte[]> OnNewByteArray { get; }

        public ScrambleService()
        {
            this.OnNewByteArray = new Subject<byte[]>();
        }


        public void Scramble(byte[] sourceArray)
        {
            for (var cursor = 0; cursor < sourceArray.Length; cursor++)
            {
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    if (sourceArray[i] != sourceArray[cursor])
                    {
                        var newArray = (byte[])sourceArray.Clone();
                        newArray[i] = sourceArray[cursor];

                        this.OnNewByteArray.OnNext(newArray);
                    }
                }
            }

            // foreach(var pkArray in this.GetPermutations<byte>(sourceArray))
            // {
            //     this.OnNewByteArray.OnNext(pkArray.ToArray());
            // }
        }

        // public IEnumerable<IEnumerable<T>> GetPermutations<T>(IEnumerable<T> collection) where T : IComparable
        // {
        //     if (!collection.Any())
        //     {
        //         return new List<IEnumerable<T>>() {Enumerable.Empty<T>() };
        //     }
        //     var sequence = collection.OrderBy(s => s).ToArray();
        //     return sequence.SelectMany(s => GetPermutations(sequence.Where(s2 => !s2.Equals(s))).Select(sq => (new T[] {s}).Concat(sq)));
        // }
    }
}
