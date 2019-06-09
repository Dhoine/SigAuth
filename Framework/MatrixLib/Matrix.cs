namespace MatrixLib
{
    public class Matrix<T> where T : new()
    {
        /// <summary>
        ///     Constrictor for matrix class
        /// </summary>
        /// <param name="n">Number of rows</param>
        /// <param name="m">Number of columns</param>
        public Matrix(int n, int m)
        {
            N = n;
            M = m;
            DataMatrix = new T[N][];
            for (var i = 0; i < N; i++) DataMatrix[i] = new T[M];
        }

        /// <summary>
        ///     Number of rows
        /// </summary>
        public int N { get; }

        /// <summary>
        ///     Number of columns
        /// </summary>
        public int M { get; }

        private T[][] DataMatrix { get; }

        /// <summary>
        ///     Get matrix element
        /// </summary>
        /// <param name="i">ColumnIndex</param>
        /// <param name="j">RowIndex</param>
        /// <returns></returns>
        public T GetItem(int i, int j)
        {
            if (i >= M || j >= N) return new T();
            return DataMatrix[j][i];
        }

        /// <summary>
        ///     SetValue
        /// </summary>
        /// <param name="i">ColumnIndex</param>
        /// <param name="j">RowIndex</param>
        /// <param name="value">Value</param>
        public void SetItem(int i, int j, T value)
        {
            DataMatrix[j][i] = value;
        }
    }
}