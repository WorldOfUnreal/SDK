using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDK.AGENT.Identity
{
    public sealed class BasicIdentity : IIdentity
    {
        static readonly Logger LOG = LoggerFactory.GetLogger(typeof(BasicIdentity));
        static JcaPEMKeyConverter pkcs8pemKeyConverter = new JcaPEMKeyConverter();
        KeyPair keyPair;
        public byte[] derEncodedPublickey;
        static BasicIdentity()
        {
            Security.AddProvider(new BouncyCastleProvider());
            pkcs8pemKeyConverter.SetProvider(BouncyCastleProvider.PROVIDER_NAME);
        }

        BasicIdentity(KeyPair keyPair, byte[] derEncodedPublickey)
        {
            this.keyPair = keyPair;
            this.derEncodedPublickey = derEncodedPublickey;
        }

        public static BasicIdentity FromPEMFile(Reader reader)
        {
            try
            {
                PEMParser pemParser = new PEMParser(reader);
                object pemObject = pemParser.ReadObject();
                if (pemObject is PrivateKeyInfo)
                {
                    PrivateKey privateKey = pkcs8pemKeyConverter.GetPrivateKey((PrivateKeyInfo)pemObject);
                    KeyFactory keyFactory = KeyFactory.GetInstance("Ed25519");
                    byte[] publicKeyBytes = ((PrivateKeyInfo)pemObject).GetPublicKeyData().GetBytes();

                    // Wrap public key in ASN.1 format so we can use X509EncodedKeySpec to read it
                    SubjectPublicKeyInfo pubKeyInfo = new SubjectPublicKeyInfo(new AlgorithmIdentifier(EdECObjectIdentifiers.id_Ed25519), publicKeyBytes);
                    X509EncodedKeySpec x509KeySpec = new X509EncodedKeySpec(pubKeyInfo.GetEncoded());
                    PublicKey publicKey = keyFactory.GeneratePublic(x509KeySpec);
                    KeyPair keyPair = new KeyPair(publicKey, privateKey);
                    return new BasicIdentity(keyPair, publicKey.GetEncoded());
                }
                else
                    throw PemError.Create(PemError.PemErrorCode.PEM_ERROR);
            }
            catch (IOException e)
            {
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
            }
            catch (NoSuchAlgorithmException e)
            {
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
            }
            catch (InvalidKeySpecException e)
            {
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
            }
        }

        public static BasicIdentity FromPEMFile(Path path)
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

        // Create a BasicIdentity from reading byte array 
        public static BasicIdentity FromPEM(byte[] keyBytes)
        {
            if (keyBytes.length == Ed25519.SECRET_KEY_SIZE + Ed25519.PUBLIC_KEY_SIZE)
            {
                try
                {
                    KeyFactory keyFactory = KeyFactory.GetInstance("Ed25519");

                    // some legacy code delivers raw private and public key pairs concatted together
                    // this is how we read only the first 32 bytes
                    byte[] privateKeyBytes = new byte[Ed25519.SECRET_KEY_SIZE];
                    System.Arraycopy(keyBytes, 0, privateKeyBytes, 0, Ed25519.SECRET_KEY_SIZE);

                    // read the remaining 32 bytes as the public key
                    byte[] publicKeyBytes = new byte[Ed25519.PUBLIC_KEY_SIZE];
                    System.Arraycopy(keyBytes, Ed25519.SECRET_KEY_SIZE, publicKeyBytes, 0, Ed25519.PUBLIC_KEY_SIZE);

                    // Wrap public key in ASN.1 format so we can use X509EncodedKeySpec to read it
                    SubjectPublicKeyInfo pubKeyInfo = new SubjectPublicKeyInfo(new AlgorithmIdentifier(EdECObjectIdentifiers.id_Ed25519), publicKeyBytes);
                    X509EncodedKeySpec x509KeySpec = new X509EncodedKeySpec(pubKeyInfo.GetEncoded());
                    PublicKey publicKey = keyFactory.GeneratePublic(x509KeySpec);

                    // Wrap private key in ASN.1 format so we can use
                    PrivateKeyInfo privKeyInfo = new PrivateKeyInfo(new AlgorithmIdentifier(EdECObjectIdentifiers.id_Ed25519), new DEROctetString(privateKeyBytes));
                    PKCS8EncodedKeySpec pkcs8KeySpec = new PKCS8EncodedKeySpec(privKeyInfo.GetEncoded());
                    PrivateKey privateKey = keyFactory.GeneratePrivate(pkcs8KeySpec);
                    KeyPair keyPair = new KeyPair(publicKey, privateKey);
                    return FromKeyPair(keyPair);
                }
                catch (InvalidKeySpecException e)
                {
                    throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
                }
                catch (IOException e)
                {
                    throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
                }
                catch (NoSuchAlgorithmException e)
                {
                    throw PemError.Create(PemError.PemErrorCode.PEM_ERROR, e);
                }
            }
            else
                throw PemError.Create(PemError.PemErrorCode.PEM_ERROR);
        }

        /// Create a BasicIdentity from a KeyPair
        public static BasicIdentity FromKeyPair(KeyPair keyPair)
        {
            PublicKey publicKey = keyPair.GetPublic();
            return new BasicIdentity(keyPair, publicKey.GetEncoded());
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
                java.security.Signature dsa;
                dsa = java.security.Signature.GetInstance("EdDSA");

                // Edwards digital signature algorithm
                dsa.InitSign(this.keyPair.GetPrivate());
                dsa.Update(msg, 0, msg.length);
                byte[] signature = dsa.Sign();
                return new Signature(this.derEncodedPublickey, signature);
            }
            catch (NoSuchAlgorithmException e)
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