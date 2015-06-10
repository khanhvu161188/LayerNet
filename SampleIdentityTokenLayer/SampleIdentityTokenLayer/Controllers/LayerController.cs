using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Owin.Security.DataHandler.Encoder;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using SampleIdentityTokenLayer.Models;
namespace SampleIdentityTokenLayer.Controllers
{
    [RoutePrefix("Layer")]
    public class LayerController : ApiController
    {
        #region fields
        public string LayerKeyId = ConfigurationManager.AppSettings["LayerKeyId"];
        public string LayerProviderId = ConfigurationManager.AppSettings["LayerProviderId"];
        #endregion

        #region method
        /// <summary>
        /// Return identity token for validate with layer
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Identity Token signed by server</returns>
        [HttpPost]
        [Route("GenerateIdentityToken")]
        public async Task<IHttpActionResult> GetLayerIdentityToken(LayerIdentityTokenRequest model)
        {
            var identityToken = GenerateIdentifyToken(model.user_id, model.nonce);
            return Json(new
            {
                identity_token = identityToken
            });

        }
        #endregion

        #region utils
        /// <summary>
        /// Generate Layer Identity Token
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="nonce"></param>
        /// <returns></returns>
        public string GenerateIdentifyToken(string userid, string nonce)
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var issueTime = DateTime.Now;
            var iat = (int)issueTime.Subtract(utc0).TotalSeconds;//issues time in second
            var exp = (int)issueTime.AddMinutes(60).Subtract(utc0).TotalSeconds;//expired time in second
            var jwtHeader = new JwtHeader
            {
                {"typ", "JWT"},
                {"alg", "RS256"},
                {"cty", "layer-eit;v=1"},
                {"kid", LayerKeyId} //String - Layer Key ID used to sign the token. This is your actual Key ID
            };
            var jwtPayload = new JwtPayload
            {
                {"iss", LayerProviderId},//The Layer Provider ID, this is your actual provider ID
                {"prn", userid}, // String - Provider's internal ID for the authenticating user e.g "CgamH6kGGl"
                {"iat", iat},// Integer - Time of Token Issuance in RFC 3339 seconds
                {"exp", exp},// Integer - Arbitrary time of Token Expiration in RFC 3339
                {"nce", nonce}//nonce from layer e.g "WxSFi3t1iu8MFBtymMBfNPUoonqXb+QO8EtrpKFdnREK44ecPTmKXglKm3KtI6S/i6/4kJam7TZStbnAW+dnsw=="
            };

            var headerSerialized = JsonConvert.SerializeObject(jwtHeader);
            var headerBytes = Encoding.UTF8.GetBytes(headerSerialized);
            var headerEncoded = TextEncodings.Base64Url.Encode(headerBytes);

            var claimsetSerialized = JsonConvert.SerializeObject(jwtPayload);
            var claimsetBytes = Encoding.UTF8.GetBytes(claimsetSerialized);
            var claimsetEncoded = TextEncodings.Base64Url.Encode(claimsetBytes);

            try
            {
                var path = HttpContext.Current.Server.MapPath(ConfigurationManager.AppSettings["LayerPemFilePath"]);
                //use BouncyCastle for load RSA private key
                var sr = new StreamReader(path);
                var pr = new PemReader(sr);
                var keyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
                var rsaPrivate = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)keyPair.Private);

                //sign data
                var rsa = new RSACryptoServiceProvider();
                rsa.ImportParameters(rsaPrivate);
                var input = String.Join(".", headerEncoded, claimsetEncoded);
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var signatureBytesTemp = rsa.SignData(inputBytes, new SHA256CryptoServiceProvider());
                var signatureEncoded = TextEncodings.Base64Url.Encode(signatureBytesTemp);
                var result = String.Join(".", headerEncoded, claimsetEncoded, signatureEncoded);
                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        #endregion
       


    }
}
