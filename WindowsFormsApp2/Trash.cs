using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperApiCSharpSample
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This sample update does a full submission update, updating listings info, images, and packages
    /// </summary>
    public class AppSubmissionUpdateSample
    {
        private ClientConfiguration ClientConfig;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="c">An instance of ClientConfiguration that contains all parameters populated</param>
        public AppSubmissionUpdateSample(ClientConfiguration c)
        {
            this.ClientConfig = c;
        }

        public void RunAppSubmissionUpdateSample()
        {
            // **********************
            //       SETTINGS
            // **********************
            var appId = this.ClientConfig.ApplicationId;
            var clientId = this.ClientConfig.ClientId;
            var clientSecret = this.ClientConfig.ClientSecret;
            var serviceEndpoint = this.ClientConfig.ServiceUrl;
            var tokenEndpoint = this.ClientConfig.TokenEndpoint;

            // Get authorization token.
            Console.WriteLine("Getting authorization token ");
            var accessToken = IngestionClient.GetClientCredentialAccessToken(
                tokenEndpoint,
                clientId,
                clientSecret).Result;

            Console.WriteLine("Getting application ");
            var client = new IngestionClient(accessToken, serviceEndpoint);
            dynamic app = client.Invoke<dynamic>(
                HttpMethod.Get,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.GetApplicationUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId),
                requestContent: null).Result;
            Console.WriteLine(app.ToString());

            // Let's get the last published submission, and print its contents, just for information.
            if (app.lastPublishedApplicationSubmission == null)
            {
                // It is not possible to create the very first submission through the API.
                throw new InvalidOperationException(
                    "You need at least one published submission to create new submissions through API.");
            }

            // Let's see if there is a pending submission. Warning! If it was created through the API,
            // it will be deleted so that we could create a new one in its stead.
            if (app.pendingApplicationSubmission != null)
            {
                var submissionId = app.pendingApplicationSubmission.id.Value as string;

                // Try deleting it. If it was NOT created via the API, then you need to manually
                // delete it from the dashboard. This is done as a safety measure to make sure that a
                // user and an automated system don't make conflicting edits.
                Console.WriteLine("Deleting the pending submission");

                client.Invoke<dynamic>(
                    HttpMethod.Delete,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.GetSubmissionUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        submissionId),
                    requestContent: null).Wait();
            }

            // Create a new submission, which will be an exact copy of the last published submission.
            Console.WriteLine("Creating a new submission");
            dynamic clonedSubmission = client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.CreateSubmissionUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId),
                requestContent: null).Result;

            // Update some property on the root submission object.
            clonedSubmission.notesForCertification = "This is a test update, updating listing info, images, and packages";

            // Now, assume we have an en-us listing. Let's try to change its description.
            clonedSubmission.listings["en-us"].baseListing.description = "This is my new en-Us description!";

            // Update images.
            // Assuming we have at least 1 image, let's delete one image.
            clonedSubmission.listings["en-us"].baseListing.images[0].fileStatus = "PendingDelete";

            var images = new List<dynamic>();
            images.Add(clonedSubmission.listings["en-us"].baseListing.images[0]);
            images.Add(
                new
                {
                    fileStatus = "PendingUpload",
                    fileName = "rectangles.png",
                    imageType = "Screenshot",
                    description = "This is a new image uploaded through the API!",
                });

            clonedSubmission.listings["en-us"].baseListing.images = JToken.FromObject(images.ToArray());

            // Update packages.
            // Let's say we want to delete the existing package.
            clonedSubmission.applicationPackages[0].fileStatus = "PendingDelete";

            // Now, let's add a new package.
            var packages = new List<dynamic>();
            packages.Add(clonedSubmission.applicationPackages[0]);
            packages.Add(
                new
                {
                    fileStatus = "PendingUpload",
                    fileName = "package.appx",
                    minimumDirectXVersion = "None",
                    minimumSystemRam = "None"
                });

            clonedSubmission.applicationPackages = JToken.FromObject(packages.ToArray());
            var clonedSubmissionId = clonedSubmission.id.Value as string;

            // Uploaded the zip archive with all new files to the SAS url returned with the submission.
            var fileUploadUrl = clonedSubmission.fileUploadUrl.Value as string;
            Console.WriteLine("FileUploadUrl: " + fileUploadUrl);
            Console.WriteLine("Uploading file");
            IngestionClient.UploadFileToBlob(@"..\..\files.zip", fileUploadUrl).Wait();

            // Update the submission.
            Console.WriteLine("Updating the submission");
            client.Invoke<dynamic>(
                HttpMethod.Put,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.UpdateUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId,
                    clonedSubmissionId),
                requestContent: clonedSubmission).Wait();

            // Tell the system that we are done updating the submission.
            // Update the submission.
            Console.WriteLine("Committing the submission");
            client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.CommitSubmissionUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    appId,
                    clonedSubmissionId),
                requestContent: null).Wait();

            // Let's periodically check the status until it changes from "CommitsStarted" to either
            // successful status or a failure.
            Console.WriteLine("Waiting for the submission commit processing to complete. This may take a couple of minutes.");
            string submissionStatus = null;
            do
            {
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                dynamic statusResource = client.Invoke<dynamic>(
                    HttpMethod.Get,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.ApplicationSubmissionStatusUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        clonedSubmissionId),
                    requestContent: null).Result;

                submissionStatus = statusResource.status.Value as string;
                Console.WriteLine("Current status: " + submissionStatus);
            }
            while ("CommitStarted".Equals(submissionStatus));

            if ("CommitFailed".Equals(submissionStatus))
            {
                Console.WriteLine("Submission has failed. Please checkt the Errors collection of the submissionResource response.");
                return;
            }
            else
            {
                Console.WriteLine("Submission commit success! Here are some data:");
                dynamic submission = client.Invoke<dynamic>(
                    HttpMethod.Get,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.GetSubmissionUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        appId,
                        clonedSubmissionId),
                    requestContent: null).Result;
                Console.WriteLine("Packages: " + submission.applicationPackages);
                Console.WriteLine("en-US description: " + submission.listings["en-us"].baseListing.description);
                Console.WriteLine("Images: " + submission.listings["en-us"].baseListing.images);
            }
        }
    }
}
Создание отправки надстройки
В следующем примере реализован класс, который использует несколько методов из API отправки в Microsoft Store для создания новой отправки надстройки. Метод RunInAppProductSubmissionCreateSample в классе выполняет следующие задачи:

Сначала метод создает новую надстройку.
Затем он создает новую отправку для надстройки.
Код передает ZIP-архив, содержащий значки для отправки, в хранилище BLOB-объектов Azure.
Затем она фиксирует новую отправку в центр партнеров.
Наконец, он периодически проверяет состояние новой отправки, пока она не будет успешно зафиксирована.
C#

Копировать
namespace DeveloperApiCSharpSample
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// Sample code for how to create add-ons, and how to create and update add-on submissions.
    /// </summary>
    public class InAppProductSubmissionCreateSample
    {
        private ClientConfiguration ClientConfig;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="c">An instance of ClientConfiguration that contains all parameters populated</param>
        public InAppProductSubmissionCreateSample(ClientConfiguration c)
        {
            this.ClientConfig = c;
        }

        public void RunInAppProductSubmissionCreateSample()
        {
            // **********************
            //       SETTINGS
            // **********************
            var appId = this.ClientConfig.ApplicationId;
            var clientId = this.ClientConfig.ClientId;
            var clientSecret = this.ClientConfig.ClientSecret;
            var serviceEndpoint = this.ClientConfig.ServiceUrl;
            var tokenEndpoint = this.ClientConfig.TokenEndpoint;

            // Get authorization token
            Console.WriteLine("Getting authorization token ");
            var accessToken = IngestionClient.GetClientCredentialAccessToken(
                tokenEndpoint,
                clientId,
                clientSecret).Result;

            Console.WriteLine("Creating a new add-on");
            dynamic newIap = new
            {
                applicationIds = new List<string>() { appId },
                productType = "Durable",
                productId = "Sample-" + Guid.NewGuid().ToString(),
            };

            var client = new IngestionClient(accessToken, serviceEndpoint);
            dynamic iapCreated = client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.CreateInAppUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant),
                requestContent: newIap).Result;
            Console.WriteLine(iapCreated.ToString());
            var iapId = iapCreated.id.Value as string;

            // Create a new submission, which will be an exact copy of the last published submission
            Console.WriteLine("Creating a new submission");
            dynamic newSubmission = new
            {
                contentType = "BookDownload",
                keywords = new List<string> { "book", "download" },
                lifeTime = "ThreeDays",
                targetPublishMode = "Immediate",
                visibility = "Public",
                pricing = new
                {
                    priceId = "Free",
                },
                listings = new Dictionary<string, dynamic>()
                {
                    {
                        "en-us",
                        new
                        {
                            description = "Sample IAP description",
                            title = "Sample IAP title",
                            icon = new
                            {
                                FileName = "icon300x300.png",
                                FileStatus = "PendingUpload",
                            },
                        }
                    }
                }
            };

            // Because it's a new add-on, we are going to create a new submission instead of
            // modifying the last published one. If you had a published add-on, you could
            // pass "null" as request body to clone the latest published submission and then
            // perform a PUT call. Alternatively, you can always post the new submission entirely
            // even if you already have a published submission but you'll have to upload the image each time.
            dynamic createdSubmission = client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.InAppSubmissionUrl,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    iapId),
                requestContent: newSubmission).Result;
            Console.WriteLine(createdSubmission);
            var submissionId = createdSubmission.id.Value as string;

            // Upload the zip archive with all new files to the SAS URL returned with the submission.
            var fileUploadUrl = createdSubmission.fileUploadUrl.Value as string;
            Console.WriteLine("FileUploadUrl: " + fileUploadUrl);
            Console.WriteLine("Uploading file");
            IngestionClient.UploadFileToBlob(@"..\..\files.zip", fileUploadUrl).Wait();

            // Tell the system that we are done updating the submission.
            // Update the submission
            Console.WriteLine("Committing the submission");
            client.Invoke<dynamic>(
                HttpMethod.Post,
                relativeUrl: string.Format(
                    CultureInfo.InvariantCulture,
                    IngestionClient.InAppProductCommitSubmissionUrlTemplate,
                    IngestionClient.Version,
                    IngestionClient.Tenant,
                    iapId,
                    submissionId),
                requestContent: null).Wait();

            // Periodically check the status until it changes from "CommitsStarted" to either
            // successful status or a failure.
            Console.WriteLine("Waiting for the submission commit processing to complete. This may take a couple of minutes.");
            string submissionStatus = null;
            do
            {
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                dynamic statusResource = client.Invoke<dynamic>(
                    HttpMethod.Get,
                    relativeUrl: string.Format(
                        CultureInfo.InvariantCulture,
                        IngestionClient.InAppSubmissionStatusUrlTemplate,
                        IngestionClient.Version,
                        IngestionClient.Tenant,
                        iapId,
                        submissionId),
                    requestContent: null).Result;

                submissionStatus = statusResource.status.Value as string;
                Console.WriteLine("Current status: " + submissionStatus);
            }
            while ("CommitStarted".Equals(submissionStatus));

            if ("CommitFailed".Equals(submissionStatus))
            {
                Console.WriteLine("Submission has failed. Please check the Errors collection of the submissionResource response.");
                return;
            }
            else
            {
                Console.WriteLine("Submission commit success!");
            }
        }

    }
}
