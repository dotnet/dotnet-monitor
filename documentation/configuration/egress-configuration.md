# Egress Configuration

When `dotnet-monitor` is used to produce artifacts such as dumps or traces, an egress provider enables the artifacts to be stored in a manner suitable for the hosting environment rather than streamed back directly.

> [!IMPORTANT]
> See [Security Considerations](../security-considerations.md#storing-configuration-secrets) for important information regarding specifying secrets in configuration.

## Azure blob storage egress provider

| Name | Type | Required | Description |
|---|---|---|---|
| accountUri | string | true | The URI of the Azure blob storage account.|
| containerName | string | true | The name of the container to which the blob will be egressed. If egressing to the root container, use the "$root" sentinel value.|
| blobPrefix | string | false | Optional path prefix for the artifacts to egress.|
| copyBufferSize | string | false | The buffer size to use when copying data from the original artifact to the blob stream.|
| accountKey | string | false | The account key used to access the Azure blob storage account; must be specified if `accountKeyName` is not specified.|
| sharedAccessSignature | string | false | The shared access signature (SAS) used to access the Azure blob and optionally queue storage accounts; if using SAS, must be specified if `sharedAccessSignatureName` is not specified.|
| accountKeyName | string | false | Name of the property in the Properties section that will contain the account key; must be specified if `accountKey` is not specified.|
| managedIdentityClientId | string | false | The ClientId of the ManagedIdentity that can be used to authorize egress. Note this identity must be used by the hosting environment (such as Kubernetes) and must also have a Storage role with appropriate permissions. |
| sharedAccessSignatureName | string | false | Name of the property in the Properties section that will contain the SAS token; if using SAS, must be specified if `sharedAccessSignature` is not specified.|
| queueName | string | false | The name of the queue to which a message will be dispatched upon writing to a blob.|
| queueAccountUri | string | false | The URI of the Azure queue storage account.|
| queueSharedAccessSignature | string | false | (6.3+) The shared access signature (SAS) used to access the Azure queue storage account; if using SAS, must be specified if `queueSharedAccessSignatureName` is not specified.|
| queueSharedAccessSignatureName | string | false | (6.3+) Name of the property in the Properties section that will contain the queue SAS token; if using SAS, must be specified if `queueSharedAccessSignature` is not specified.|
| metadata | Dictionary<string, string> | false | A mapping of metadata keys to environment variable names. The values of the environment variables will be added as metadata for egressed artifacts.|

> [!NOTE]
> Starting with `dotnet monitor` 7.0, all built-in metadata keys are prefixed with `DotnetMonitor_`; to avoid metadata naming conflicts, avoid prefixing your metadata keys with `DotnetMonitor_`.

### Example azureBlobStorage provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

### Example azureBlobStorage provider with queue

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "AzureBlobStorage": {
              "monitorBlob": {
                  "accountUri": "https://exampleaccount.blob.core.windows.net",
                  "containerName": "dotnet-monitor",
                  "blobPrefix": "artifacts",
                  "accountKeyName": "MonitorBlobAccountKey",
                  "queueAccountUri": "https://exampleaccount.queue.core.windows.net",
                  "queueName": "dotnet-monitor-queue"
              }
          },
          "Properties": {
              "MonitorBlobAccountKey": "accountKey"
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__AzureBlobStorage__monitorBlob__accountUri: "https://exampleaccount.blob.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__containerName: "dotnet-monitor"
  Egress__AzureBlobStorage__monitorBlob__blobPrefix: "artifacts"
  Egress__AzureBlobStorage__monitorBlob__accountKeyName: "MonitorBlobAccountKey"
  Egress__AzureBlobStorage__monitorBlob__queueAccountUri: "https://exampleaccount.queue.core.windows.net"
  Egress__AzureBlobStorage__monitorBlob__queueName: "dotnet-monitor-queue"
  Egress__Properties__MonitorBlobAccountKey: "accountKey"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountUri
    value: "https://exampleaccount.blob.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__containerName
    value: "dotnet-monitor"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__blobPrefix
    value: "artifacts"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__accountKeyName
    value: "MonitorBlobAccountKey"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueAccountUri
    value: "https://exampleaccount.queue.core.windows.net"
  - name: DotnetMonitor_Egress__AzureBlobStorage__monitorBlob__queueName
    value: "dotnet-monitor-queue"
  - name: DotnetMonitor_Egress__Properties__MonitorBlobAccountKey
    value: "accountKey"
  ```
</details>

#### azureBlobStorage Queue Message Format

The Queue Message's payload will be the blob name (`<BlobPrefix>/<ArtifactName>`; using the above example with an artifact named `mydump.dmp`, this would be `artifacts/mydump.dmp`) that is being egressed to blob storage. This is designed to be easily integrated into an Azure Function that triggers whenever a new message is added to the queue, providing you with the contents of the artifact as a stream. See [Azure Blob storage input binding for Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-blob-input?tabs=csharp#example) for an example.

## S3 storage egress provider

First Available: 8.0

| Name | Type | Required | Description |
|---|---|---|---|
| endpoint | string | false | An optional endpoint of S3 storage service. Can be left empty in case of using AWS. |
| bucketName | string | true | The name of the S3 Bucket to which the blob will be egressed. |
| accessKeyId | string | false | The AWS AccessKeyId for IAM user to login.  |
| secretAccessKey | string | false | The AWS SecretAccessKey associated AccessKeyId for IAM user to login. To login by access key id the 'secretAccessKey' must be set. |
| sessionToken | string | false | (10.1+) The AWS SessionToken that accompanies temporary (STS-issued) credentials, e.g. those returned by `sts:AssumeRole` or by an S3-compatible service that issues short-lived credentials. When set, both 'accessKeyId' and 'secretAccessKey' must also be set. |
| awsProfileName | string | false | The AWS profile name to be used for login. |
| awsProfilePath | string | false | The AWS profile path, if profile details not stored in default path. |
| regionName | string | false | A Region is a named set of AWS resources in the same geographical area. This option specifies the region to connect to. If the Endpoint is specified, this is the AuthenticationRegion; otherwise, it is the RegionEndpoint. |
| preSignedUrlExpiry | TimeStamp? | false | When specified, a pre-signed url is returned after successful upload; this value specifies the amount of time the generated pre-signed url should be accessible. The value has to be between 1 minute and 1 day. |
| forcePathStyle | bool | false | The boolean flag set for AWS connection configuration ForcePathStyle option. |
| disablePayloadSigning | bool | false | (10.1+) Sends request payloads with an `UNSIGNED-PAYLOAD` signature, suppressing flexible-checksum trailers and response checksum validation. Required by S3-compatible services that do not implement chunked or trailered payloads (for example Cloudflare R2). The endpoint must use HTTPS. |
| copyBufferSize | int | false | The buffer size to use when copying data from the original artifact to the blob stream. There is a minimum size of 5 MB which is set when the given value is lower.|
| useKmsEncryption | bool | false | (9.0 Preview 6+) A boolean flag which controls whether the Egress should use KMS server side encryption. |
| kmsEncryptionKey | string | false | (9.0 Preview 6+) If UseKmsEncryption is true, this specifies the arn of the "customer managed" KMS encryption key to be used for server side encryption. If no value is set for this field then S3 will use an AWS managed key for KMS encryption. |

### Example S3 storage provider

<details>
  <summary>JSON with password</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "http://localhost:9000",
                  "bucketName": "myS3Bucket",
                  "accessKeyId": "minioUser",
                  "secretAccessKey": "mySecretPassword",
                  "regionName": "us-east-1",
                  "preSignedUrlExpiry" : "00:15:00",
                  "copyBufferSize": 1024
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>JSON with customer managed KMS encryption</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "http://localhost:9000",
                  "bucketName": "myS3Bucket",
                  "useKmsEncryption": true,
                  "kmsEncryptionKey": "arn:aws:kms:{region}:{account-id}:key/{resource-id}"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes Secret</summary>

  ```sh
  #!/bin/sh
  kubectl create secret generic my-s3-secrets \
  --from-literal=Egress__S3Storage__monitorS3Blob__bucketName=myS3Bucket \
  --from-literal=Egress__S3Storage__monitorS3Blob__accessKeyId=minioUser \
  --from-literal=Egress__S3Storage__monitorS3Blob__secretAccessKey=mySecretPassword \
  --from-literal=Egress__S3Storage__monitorS3Blob__regionName=us-east-1 \
  --dry-run=client -o yaml | kubectl apply -f -
 ```
</details>

### Authenticating to S3 using temporary credentials

First Available: 10.1

Some credential issuers (`sts:AssumeRole`, HashiCorp Vault, and several S3-compatible services) only ever hand out short-lived credentials, which consist of an access key id, a secret access key **and** a session token. All three must be presented on every request; a request signed with only the first two is rejected by the service.

Set `sessionToken` alongside `accessKeyId` and `secretAccessKey` to use such credentials. `endpoint`, `regionName` and `forcePathStyle` are honored as usual, so this works against a custom S3-compatible endpoint.

<details>
  <summary>JSON with a session token</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "https://s3.example.com",
                  "bucketName": "myS3Bucket",
                  "accessKeyId": "myTemporaryAccessKeyId",
                  "secretAccessKey": "myTemporarySecretAccessKey",
                  "sessionToken": "mySessionToken",
                  "regionName": "auto",
                  "forcePathStyle": true
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes Secret</summary>

  ```sh
  #!/bin/sh
  kubectl create secret generic my-s3-secrets \
  --from-literal=Egress__S3Storage__monitorS3Blob__bucketName=myS3Bucket \
  --from-literal=Egress__S3Storage__monitorS3Blob__accessKeyId=myTemporaryAccessKeyId \
  --from-literal=Egress__S3Storage__monitorS3Blob__secretAccessKey=myTemporarySecretAccessKey \
  --from-literal=Egress__S3Storage__monitorS3Blob__sessionToken=mySessionToken \
  --from-literal=Egress__S3Storage__monitorS3Blob__regionName=auto \
  --dry-run=client -o yaml | kubectl apply -f -
 ```
</details>

> **Note:** Temporary credentials expire. It is the responsibility of the credential source (for example a sidecar that refreshes a mounted secret) to keep the configured values current; `dotnet monitor` does not renew them.

### Egressing to S3-compatible services without chunked payload support

First Available: 10.1

By default the AWS SDK uploads with a signed streaming payload (`STREAMING-AWS4-HMAC-SHA256-PAYLOAD`)
and attaches a flexible checksum as an `aws-chunked` trailer. Several S3-compatible services implement
neither, and reject every upload:

```
S3 storage egress failed: STREAMING-AWS4-HMAC-SHA256-PAYLOAD-TRAILER not implemented
```

Because artifacts are streamed as a multi-part upload, this affects all of them — dumps, traces, logs
and GC dumps alike. Cloudflare R2 is the common case.

Set `disablePayloadSigning` to `true` to send `UNSIGNED-PAYLOAD` instead and drop the checksum trailer.
The request is then authenticated by its SigV4-signed headers, and the body is protected by TLS rather
than by the payload signature — so the endpoint must be HTTPS, which is validated at startup.

<details>
  <summary>JSON for an endpoint without chunked payload support</summary>

  ```json
  {
      "Egress": {
          "S3Storage": {
              "monitorS3Blob": {
                  "endpoint": "https://<accountid>.r2.cloudflarestorage.com",
                  "bucketName": "myS3Bucket",
                  "accessKeyId": "myAccessKeyId",
                  "secretAccessKey": "mySecretAccessKey",
                  "regionName": "auto",
                  "disablePayloadSigning": true
              }
          }
      }
  }
  ```
</details>

> **Note:** Leave this off for Amazon S3, which implements both features. Turning it off where it is
> not needed loses the end-to-end integrity check that the payload signature and checksum provide.

### S3 endpoint handling on every authentication path

First Available: 10.1

> **Behavior change:** `endpoint`, `regionName` and `forcePathStyle` are applied on **every**
> authentication path. Previously they were only applied when `accessKeyId` and `secretAccessKey`
> were both set, and were silently ignored when authenticating via `awsProfileName` or via the
> default credential chain — which made those paths unusable against a custom S3-compatible
> endpoint (the client failed with `No RegionEndpoint or ServiceURL configured`).
>
> If you authenticate with a profile or the default chain, set no `endpoint`, and previously
> relied on the region coming from the profile or `AWS_REGION` while *also* having a stale
> `regionName` configured, the configured `regionName` now wins. Note that an unrecognized
> region name does not fail fast: `RegionEndpoint.GetBySystemName` returns a placeholder region
> and the failure surfaces later, when the request is made.

### Authenticating to S3 using service accounts

First Available: 9.0 Preview 5


If running workloads in Kubernetes it is common to authenticate with AWS via Kubernetes service accounts ([AWS Documentation](https://docs.aws.amazon.com/eks/latest/userguide/pod-configuration.html)). This is supported in dotnet monitor if none of: `accessKeyId`, `secretAccessKey`, `awsProfileName` are specified. In this case dotnet monitor will fallback to load credentials to login using AWS default defined environment variables, this means that workloads running in EKS can utilize service accounts as discussed in the above AWS documentation.

Specifically the use of service accounts set the following environment variables which are detected by AWS SDK and used for authentication as a fallback:
 - AWS_REGION
 - AWS_ROLE_ARN
 - AWS_WEB_IDENTITY_TOKEN_FILE

## Filesystem egress provider

| Name | Type | Description |
|---|---|---|
| directoryPath | string | The directory path to which the stream data will be egressed.|
| intermediateDirectoryPath | string | The directory path to which the stream data will initially be written; if specified, the file will then be moved/renamed to the directory specified in 'directoryPath'.|

### Example fileSystem provider

<details>
  <summary>JSON</summary>

  ```json
  {
      "Egress": {
          "FileSystem": {
              "monitorFile": {
                  "directoryPath": "/artifacts",
                  "intermediateDirectoryPath": "/intermediateArtifacts"
              }
          }
      }
  }
  ```
</details>

<details>
  <summary>Kubernetes ConfigMap</summary>

  ```yaml
  Egress__FileSystem__monitorFile__directoryPath: "/artifacts"
  Egress__FileSystem__monitorFile__intermediateDirectoryPath: "/intermediateArtifacts"
  ```
</details>

<details>
  <summary>Kubernetes Environment Variables</summary>

  ```yaml
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__directoryPath
    value: "/artifacts"
  - name: DotnetMonitor_Egress__FileSystem__monitorFile__intermediateDirectoryPath
    value: "/intermediateArtifacts"
  ```
</details>
