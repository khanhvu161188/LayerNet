namespace SampleIdentityTokenLayer.Models
{
    /// <summary>
    /// model for get layer identity code
    /// </summary>
    public class LayerIdentityTokenRequest
    {
        /// <summary>
        /// Nonce created by Layer client
        /// </summary>
        public string nonce { get; set; }

        /// <summary>
        /// User id used for register with Layer server
        /// </summary>
        public string user_id { get; set; }
    }
}