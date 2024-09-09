# API Key Format
API Keys or MonitorApiKeys used in `dotnet monitor` are JSON Web Tokens or JWTs as defined by [RFC 7519: JSON Web Token (JWT)](https://datatracker.ietf.org/doc/html/rfc7519).
> [!IMPORTANT]
> Because the API Key is a `Bearer` token, it should be treated as a secret and always transmitted over `TLS` or another protected protocol.

It is possible to make your own API Keys for `dotnet monitor` by following the format as defined below. Although, it is recommended to use the `generatekey` command unless you have a specific reason to make your own key.

## Token Format
To use a JWT for authentication with `dotnet monitor`, the token must follow certain constraints. These constraints will be validated by `dotnet monitor` at configuration load time, authentication time, and authorization time.

For this example, let's consider the API Key given on the [Authentication page](authentication.md). This token consists of 3 parts: Header, Payload, and Signature; explained in detail later. This is the entire portion passed as a `Bearer` type in the `Authorization` HTTP header:
```yaml
eyJhbGciOiJFUffffffffffffCI6IkpXVCJ9.eyJhdWQiOiJodffffffffffffGh1Yi5jb20vZG90bmV0L2RvdG5ldC1tb25pdG9yIiwiaXNzIjoiaHR0cHM6Ly9naXRodWIuY29tL2RvdG5ldC9kb3RuZXQtbW9uaXRvci9nZW5lcmF0ZWtleStNb25pdG9yQXBpS2V5Iiwic3ViIjoiYWU1NDczYjYtOGRhZC00OThkLWI5MTUtNTNiOWM2ODQwMDBlIn0.RZffffffffffff_yIyApvFKcxFpDJ65HJZek1_dt7jCTCMEEEffffffffffffR08OyhZZHs46PopwAsf_6fdTLKB1UGvLr95volwEwIFnHjdvMfTJ9ffffffffffffAU
```
> [!NOTE]
> While all values provided in this document are the correct length and format, the raw values have been edited to prevent this public example being used as a dotnet-monitor configuration.

### Header
The header (decoded from the token above) must contain at least 2 elements: `alg` (or [Algorithm](https://www.rfc-editor.org/rfc/rfc7518.html#section-3.1)), and `typ` (or [Type](https://datatracker.ietf.org/doc/html/rfc7519#section-5.1)). `dotnet monitor` expects the `typ` to always be `JWT` for a JSON Web Token. `dotnet monitor` supports 6 `alg` values: `ES256`, `ES384`, `ES512`, `RS256`, `RS384`, and `RS512`.

```json
{
  "alg": "ES384",
  "typ": "JWT"
}
```
> [!NOTE]
> The `alg` requirement is designed to enforce `dotnet monitor` to use public/private key signed tokens. This allows the key that is stored in configuration (as `Authentication__MonitorApiKey__PublicKey`) to only contain public key information and thus does not need to be kept secret.

### Payload
The payload (also decoded from the token above) must contain at least 4 elements: `aud` , `exp`  `iss`, and `sub`.
- The `aud` field ([Audience](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1.3)) must to always be `https://github.com/dotnet/dotnet-monitor` which signals that the token is intended for dotnet-monitor.
- The `exp` field ([Expiration](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1.4)) is the expiration date of the token in the form of an integer that is the number of seconds since Unix epoch.
- The `iss` field ([Issuer](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1.1)) is any non-empty string defined in `Authentication__MonitorApiKey__Issuer`, this is used to validate that the token provided was produced by the expected issuer. If `Authentication__MonitorApiKey__Issuer` is not specified, the value in the token must be `https://github.com/dotnet/dotnet-monitor/generatekey+MonitorApiKey`.
- The `sub` field ([Subject](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1.2)) is any non-empty string defined in `Authentication__MonitorApiKey__Subject`, this is used to validate that the token provided is for the expected instance and is user-defined in configuration.

When using the `generatekey` command, the `sub` field will be a randomly-generated `Guid` but the `sub` field may be any non-empty string that matches the configuration. The `iss` (or [Issuer](https://datatracker.ietf.org/doc/html/rfc7519#section-4.1.1)) field will be set to `https://github.com/dotnet/dotnet-monitor/generatekey+MonitorApiKey` to specify the source of the token.

```json
{
  "aud": "https://github.com/dotnet/dotnet-monitor",
  "exp": "1713799523",
  "iss": "https://github.com/dotnet/dotnet-monitor/generatekey+MonitorApiKey",
  "sub": "ae5473b6-8dad-498d-b915-ffffffffffff"
}
```

### Signature
JSON web tokens may be cryptographically signed; `dotnet monitor` **requires all** tokens to be signed and supports `RSA PKCS1 v1.5` and `ECDSA` signed tokens, these are tokens with `RS*` and `ES*` as `alg` values. `dotnet monitor` needs the public key portion of this cryptographic material in order to validate the token. See the [Providing a Public Key](#providing-a-public-key) section for information on how to encode a key.

## Providing a Public Key

The public key is provided to `dotnet monitor` as a JSON Web Key or JWK, as defined by [RFC 7517: JSON Web Key (JWK)](https://www.rfc-editor.org/rfc/rfc7517.html). This key must be serialized as JSON and then Base64 URL encoded into a single string.

`dotnet monitor` imposes the following constraints on JWKs:
- The key **should not** have private key data. The key is only used for signature verification, and thus private key parameters are not needed. A warning message will be shown if the private key data is included.
- The `kty` (or [Key Type](https://www.rfc-editor.org/rfc/rfc7517.html#section-4.1)) must be `RSA` for a RSA public key or `EC` for an elliptic-curve public key.

The key used for the token in this example is:

```json
{
  "AdditionalData":{},
  "Crv":"P-384",
  "KeyOps":[],
  "Kty":"EC",
  "X":"HoffffffffffffuHyjH_57Yf4AkPLEhI5QOTnRugE192Xz_VqcffffffffffffOj",
  "X5c":[],
  "Y":"JyffffffffffffhzyV-VCMdUttelaY2a8WmileII4MzaYp9j6EffffffffffffFi",
  "KeySize":384,
  "HasPrivateKey":false,
  "CryptoProviderFactory": {
    "CryptoProviderCache":{},
    "CacheSignatureProviders":true,
    "SignatureProviderObjectPoolCacheSize":32
  }
}
```
The JWK above is then Base64 URL encoded into the following value which is passed to `dotnet monitor` as `Authentication__MonitorApiKey__PublicKey`
```yaml
eyffffffffffffFsRGF0YSI6e30sIkNydiI6IlAtMzg0IiwiS2V5T3BzIjpbXSwiS3R5IjoiRUMiLCJYIjoiTnhIRnhVZ19QM1dhVUZWVzk0U3dUY3FzVk5zNlFLYjZxc3AzNzVTRmJfQ3QyZHdpN0RWRl8tUTVheERtYlJuWSIsIlg1YyI6W10sIlkiOiJmMXBDdmNoUkVpTWEtc1h6SlZQaS02YmViMHdrZmxfdUZBN0Vka2dwcjF5N251Wmk2cy1NcHl5RzhKdVFSNWZOIiwiS2V5U2l6ZSI6Mzg0LCJIYXNQcml2YXRlS2V5IjpmYWxzZSwiQ3J5cHRvUHJvdmlkZXJGYWN0b3J5Ijp7IkNyeXB0b1Byb3ZpZGVyQ2FjaGUiOnt9LCJDYWNoZVNpZ25hdHVyZVByb3ZpZGVycyI6dHJ1ZSwiU2lnbmF0dXJlUHJvdmlkZXJPYmplY3RQb29sQ2FjaGffffffffffff19
```
