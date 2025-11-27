namespace OrderBE.Exceptions
{
    /// <summary>
    /// エンティティが見つからない場合にスローされるカスタム例外
    /// Repository層でデータベース検索時に該当データが存在しない場合に使用
    /// </summary>
    public class EntityNotFoundException : Exception
    {
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public EntityNotFoundException() : base()
        {
        }

        /// <summary>
        /// メッセージ付きコンストラクタ
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        public EntityNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// メッセージと内部例外付きコンストラクタ
        /// </summary>
        /// <param name="message">例外メッセージ</param>
        /// <param name="innerException">内部例外</param>
        public EntityNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
