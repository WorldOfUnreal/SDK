using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public sealed class Secp256k1Identity : IIdentity
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(Secp256k1Identity));
        static JcaPEMKeyConverter jcaPemKeyConverter = new JcaPEMKeyConverter();
        KeyPair keyPair;
        public byte[] derEncodedPublickey;
        static Secp256k1Identity()
        {
            Security.AddProvider(new BouncyCastleProvider());
            jcaPemKeyConverter.SetProvider(BouncyCastleProvider.PROVIDER_NAME);
        }

        Secp256k1Identity(KeyPair keyPair, byte[] derEncodedPublickey)
        {
            this.keyPair = keyPair;
            this.derEncodedPublickey = derEncodedPublickey;
        }

        public static Secp256k1Identity FromPEMFile(Reader reader)
        {
            try
            {
                PEMParser pemParser = new PEMParser(reader);
                object pemObject = pemParser.ReadObject();
                if (pemObject is PEMKeyPair)
                {
                    KeyPair keyPair = jcaPemKeyConverter.GetKeyPair((PEMKeyPair)pemObject);
                    PublicKey publicKey = keyPair.GetPublic();
                    return new Secp256k1Identity(keyPair, publicKey.GetEncoded());
                }
                else
                    throw PemError.Create(PemError.PemErrorCode.PEM_ERROR);
            }
            catch (IOException e)
            {
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
            }
        }

        public static Secp256k1Identity FromPEMFile(Path path)
        {
            try
            {
                Reader reader = Files.NewBufferedReader(path);
                return FromPEMFile(reader);
            }
            catch (IOException e)
            {
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
            }
        }

        /// Create a Secp256k1Identity from a KeyPair
        public static Secp256k1Identity FromKeyPair(KeyPair keyPair)
        {
            PublicKey publicKey = keyPair.GetPublic();
            return new Secp256k1Identity(keyPair, publicKey.GetEncoded());
        }

        public override Principal Sender()
        {
            return Principal.SelfAuthenticating(derEncodedPublickey);
        }

        public override Signature Sign(byte[] msg)
        {
            try
            {

                // Generate new signature
                java.security.Signature dsa = java.security.Signature.GetInstance("SHA256withPLAIN-ECDSA", "BC");

                // ECDSA digital signature algorithm
                dsa.InitSign(this.keyPair.GetPrivate());
                dsa.Update(msg);
                byte[] signature = dsa.Sign();
                return new Signature(this.derEncodedPublickey, signature);
            }
            catch (NoSuchAlgorithmException e)
            {
                throw PemError.Create(PemError.PemErrorCode.ERROR_STACK, e);
            }
            catch (NoSuchProviderException e)
            {
                throw PemError.Create(PemError.PemErrorCode.ERROR_STACK, e);
            }
            catch (InvalidKeyException e)
            {
                throw PemError.Create(PemError.PemErrorCode.ERROR_STACK, e);
            }
            catch (SignatureException e)
            {
                throw PemError.Create(PemError.PemErrorCode.ERROR_STACK, e);
            }
        }
    }
}