namespace App.Core.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Batches an enumerable into an enumerable of enumerables by batchSize parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batchSize = 10) =>
            items
                .Select((item, ix) => new {item, ix})
                .GroupBy(x => x.ix / batchSize)
                .Select(g => g.Select(x => x.item));

        /// <summary>
        /// Helper method for selecting with an index for each item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));
    }
}
