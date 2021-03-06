using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using OCBC_Joint_Account_Application.Models;
using OCBC_Joint_Account_Application.DAL;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Newtonsoft.Json;
using RestSharp;
using System.IO;
using System.Threading;
using System.Text;
using OCBC_Online_Joint_Account.Models;
using System.Globalization;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Newtonsoft.Json.Linq;
using OCBC_Online_Joint_Account.DAL;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text.RegularExpressions;
using SimpleTCP;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;


namespace OCBC_Joint_Account_Application.Controllers
{
    public class Account360Controller : Controller
    {
        private SingpassDAL singpassContext = new SingpassDAL();
        private CustomerDAL customerContext = new CustomerDAL();
        private CustApplicationDAL custApplicationContext = new CustApplicationDAL();
        private ApplicationDAL applicationContext = new ApplicationDAL();
        private BankAccountDAL bankContext = new BankAccountDAL();
        private CustomerAccountsDAL custAccContext = new CustomerAccountsDAL();

        private List<SelectListItem> Salutation = new List<SelectListItem>();
        private List<SelectListItem> CountryOfBirth = new List<SelectListItem>();
        private List<SelectListItem> Nationality = new List<SelectListItem>();
        private List<SelectListItem> Gender = new List<SelectListItem>();
        private List<SelectListItem> MaritialStatus = new List<SelectListItem>();
        private List<SelectListItem> AnnualIncome = new List<SelectListItem>();
        private List<SelectListItem> Occupation = new List<SelectListItem>();
        private List<SelectListItem> YearsInEmployment = new List<SelectListItem>();
        private List<TaxResidency> TaxResidencyList = new List<TaxResidency>();

        const string SUBSCRIPTION_KEY = "c9f874bd47a34fe68baf91aee0f144fe";
        const string ENDPOINT = "https://face-verify-ocbc.cognitiveservices.azure.com/";
        private string SourceUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", "selfie.jpeg");
        private string TargetUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", "NRIC Front.png");
        const string RECOGNITION_MODEL4 = RecognitionModel.Recognition04;

        public Account360Controller()
        {
            //Populate Salutation
            Salutation.Add(new SelectListItem { Value = "Dr", Text = "Dr" });
            Salutation.Add(new SelectListItem { Value = "Mdm", Text = "Mdm" });
            Salutation.Add(new SelectListItem { Value = "Miss", Text = "Miss" });
            Salutation.Add(new SelectListItem { Value = "Mr", Text = "Mr" });
            Salutation.Add(new SelectListItem { Value = "Mrs", Text = "Mrs" });
            Salutation.Add(new SelectListItem { Value = "Ms", Text = "Ms" });

            //Populate Gender
            Gender.Add(new SelectListItem { Value = "Male", Text = "Male" });
            Gender.Add(new SelectListItem { Value = "Female", Text = "Female" });

            //Populate Maritial Status
            MaritialStatus.Add(new SelectListItem { Value = "Single", Text = "Single" });
            MaritialStatus.Add(new SelectListItem { Value = "Married", Text = "Married" });
            MaritialStatus.Add(new SelectListItem { Value = "Widowed", Text = "Widowed" });
            MaritialStatus.Add(new SelectListItem { Value = "Divorced", Text = "Divorced" });
            MaritialStatus.Add(new SelectListItem { Value = "Married but seprated", Text = "Married but seprated" });

            //Populate Annual Income
            AnnualIncome.Add(new SelectListItem { Value = "1", Text = "Less Than 30,000" });
            AnnualIncome.Add(new SelectListItem { Value = "2", Text = "30,000 - 49,000" });
            AnnualIncome.Add(new SelectListItem { Value = "3", Text = "50,000 - 99,999" });
            AnnualIncome.Add(new SelectListItem { Value = "4", Text = "100,000 - 149,000" });
            AnnualIncome.Add(new SelectListItem { Value = "5", Text = "150,000 - 199,000" });
            AnnualIncome.Add(new SelectListItem { Value = "6", Text = "Above 200,000" });

            //Populate Occupation
            Occupation.Add(new SelectListItem { Value = "1", Text = "Architect" });
            Occupation.Add(new SelectListItem { Value = "2", Text = "Doctor/Dentist" });
            Occupation.Add(new SelectListItem { Value = "3", Text = "Engineer" });
            Occupation.Add(new SelectListItem { Value = "4", Text = "IT Professional" });
            Occupation.Add(new SelectListItem { Value = "5", Text = "Legal Professional/Lawyer" });
            Occupation.Add(new SelectListItem { Value = "6", Text = "Student" });
            Occupation.Add(new SelectListItem { Value = "7", Text = "Teacher" });

            //Populate Years In Employment
            YearsInEmployment.Add(new SelectListItem { Value = "< 1", Text = "< 1" });
            for (int i = 1; i <= 40; i++)
            {
                YearsInEmployment.Add(new SelectListItem { Value = Convert.ToString(i), Text = Convert.ToString(i) });
            }
            YearsInEmployment.Add(new SelectListItem { Value = "> 40", Text = "> 40" });

            //Populate Tax Residency list
            TaxResidencyList.Add(new TaxResidency
            {
                Country = "Singapore",
                Selected = false
            });
            TaxResidencyList.Add(new TaxResidency
            {
                Country = "United States",
                Selected = false
            });
            TaxResidencyList.Add(new TaxResidency
            {
                Country = "Other country(s)",
                Selected = false
            });
        }

        /**==========================
              APPLYONLINE.CSHTML
         ==========================**/
        public ActionResult ApplyOnline(int? AT, string JAC)
        {
            HttpContext.Session.SetString("PageType", "Account360");
            HttpContext.Session.SetInt32("AccountTypeID", 2);
            HttpContext.Session.SetString("FirstLoad", "false");


            if (JAC != null)
            {
                HttpContext.Session.SetString("JAC", JAC);
                InsertQRForJointApplicant(AT, JAC);
                return RedirectToAction("ApplyOnline", "Account360");
            }
            else if(HttpContext.Session.GetString("ContinueWifi") != null)
            {
                string phrase = HttpContext.Session.GetString("ContinueWifi");
                string[] words = phrase.Split(',');
                HttpContext.Session.SetString("Salutation_WIFI", words[0]);
                HttpContext.Session.SetString("FullName_WIFI", words[1]);
            }

            checkJAC(HttpContext.Session.GetString("JAC"));
            if (ResponseQR() == "ContinueOnWeb")
            {
                return RedirectToAction("Identity", "Account360");
            }
            else if(ResponseQR() == "ContinueOnMobile")
            {
                return RedirectToAction("ContinueOnMobile", "Account360");
            }
            return View();
        }


        /**==========================
              IDENTITY.CSHTML
        ==========================**/

        public ActionResult Identity()
        {
            if (HttpContext.Session.GetString("Applicant") == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if(isCorrectApplicantLogin() == false)
            {
                return RedirectToAction("Denied", "Account360");
            }

            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("PageType", "Account360");

            //Get Mobile Number
            if (HttpContext.Session.GetString("ApplyMethod") == "iBanking" || HttpContext.Session.GetString("ApplyMethod") == "QR")
            {
                foreach (Customer c in customerContext.GetCustomerByNRIC(HttpContext.Session.GetString("iBankingLogin")))
                {
                    HttpContext.Session.SetString("mobileNum", c.ContactNo);
                }
            }
            else
            {
                foreach (Singpass sp in singpassContext.GetSingpassByNRIC(HttpContext.Session.GetString("Applicant")))
                {
                    HttpContext.Session.SetString("mobileNum", sp.MobileNum);
                }
            }
    
            return View();
        }

        [HttpPost]
        public ActionResult Identity(Account360ViewModel a360, int? id)
        {
            if(id == 1)
            {
                //Generate 6-digit OTP
                Random rnd = new Random();
                int OTP = rnd.Next(100000, 999999);
                string mobileNum = HttpContext.Session.GetString("mobileNum");

                //OTP API by Twilio
                var accountSid = "AC900a65cf35b142ba9d231968f7975595";
                var authToken = "6dbf2032023e857f59f960615d9afb65";
                TwilioClient.Init(accountSid, authToken);
                var messageOptions = new CreateMessageOptions(new PhoneNumber("+65" + mobileNum));
                messageOptions.MessagingServiceSid = "MG9dc1a6ffbac9048864eaadfda51637fc";
                messageOptions.Body = "OCBC: Your One-Time Password is " + OTP + ".\nUse it to continue with the application.";
                var message = MessageResource.Create(messageOptions);

                HttpContext.Session.SetInt32("OTP", OTP);
                ViewData["MobileNum"] = "Your OTP is sent to +65 " + mobileNum;
            }
            else
            {
                checkJAC(HttpContext.Session.GetString("JAC"));
                HttpContext.Session.SetString("PageType", "Account360");

                if (a360.OTP == HttpContext.Session.GetInt32("OTP"))
                {
                    Console.WriteLine("HttpContext: " + HttpContext.Session.GetString("CustSingpass"));
                    if (HttpContext.Session.GetString("CustSingpass") == "existingCustomer" || HttpContext.Session.GetString("ApplyMethod") == "iBanking" || HttpContext.Session.GetString("ApplyMethod") == "QR")
                    {
                        return RedirectToAction("JointApplicant", "Account360");
                    }
                    else
                    {
                        return RedirectToAction("Form", "Account360");
                    }
                }

                ViewData["Invalid"] = "Invalid OTP";
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IdentityFace(string custNRIC, Account360ViewModel a360)
        {
            custNRIC = HttpContext.Session.GetString("Applicant");
            custNRIC = custNRIC + ".jpg";
            string fileLocation = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\nric-verify\\");
            string savePath = "";

            if (a360.SelfieURL != null)
            {
                try
                {
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\nric-verify\\selfie.jpeg");
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        byte[] imgBytes = Convert.FromBase64String(a360.SelfieURL.Split(',')[1]);
                        fileSteam.Write(imgBytes);
                    }
                }
                catch (IOException)
                {
                    return View("Identity", a360);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Identity", a360);
                }
            }

            foreach (string filename in Directory.GetFiles(fileLocation))
            {
                if (custNRIC == Path.GetFileName(filename))
                {
                    string SourceUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\nric-verify\\", Path.GetFileName(filename));
                    string TargetUrl = savePath;
                    double confidence = 0;
                    // use face api to check selfie against face
                    IFaceClient faceClient = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
                    try
                    {
                        double faceVerify = await FindSimilar(faceClient, SourceUrl, TargetUrl, RECOGNITION_MODEL4);
                        if (faceVerify == -1)
                        {
                            TempData["FaceError"] = "Sorry an error occured. Please take the selfie picture again.";
                            return RedirectToAction("Identity", "Account360");
                        }
                        confidence = faceVerify;
                    }
                    catch (IOException)
                    {
                        return RedirectToAction("Identity", "Account360");
                    }
                    catch (Exception)
                    {
                        TempData["FaceError"] = "Sorry an error occured. Please take the selfie picture again.";
                        return RedirectToAction("Identity", "Account360");
                    }

                    if (System.IO.File.Exists(savePath))
                        System.IO.File.Delete(savePath);

                    if (confidence >= 0.4)
                    {
                        return RedirectToAction("JointApplicant", "Account360");
                    }
                    else
                    {
                        TempData["FaceError"] = "Sorry but your face is not identical with your Singpass records. Please try again.";
                        return RedirectToAction("Identity", "Account360");
                    }
                }
                else
                {
                    continue;
                }
            }
            return RedirectToAction("Identity", "Account360");
        }

        /**==========================
                 FORM.CSHTML
        ==========================**/

        public async Task<ActionResult> Form()
        {
            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("PageType", "Account360");

            //Populate CountryOfBirth & Nationality
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://raw.githubusercontent.com/");
            HttpResponseMessage response = await client.GetAsync("Dinuks/country-nationality-list/master/countries.json");
            if (response.IsSuccessStatusCode)
            {
                string data = await response.Content.ReadAsStringAsync();
                List<Country> country = JsonConvert.DeserializeObject<List<Country>>(data);

                foreach (Country c in country)
                {
                    CountryOfBirth.Add(new SelectListItem { Value = c.en_short_name, Text = c.en_short_name });
                    Nationality.Add(new SelectListItem { Value = c.nationality, Text = c.nationality });
                }
            }

            ViewData["CountryOfBirth"] = CountryOfBirth;
            ViewData["Nationality"] = Nationality;
            ViewData["Salutation"] = Salutation;
            ViewData["Gender"] = Gender;
            ViewData["MaritialStatus"] = MaritialStatus;
            ViewData["AnnualIncome"] = AnnualIncome;
            ViewData["Occupation"] = Occupation;
            ViewData["YearsInEmployment"] = YearsInEmployment;

            Account360ViewModel ac360 = new Account360ViewModel();
            ac360.TaxResidencyList = TaxResidencyList;
            

            // Check for Singpass then run code to pull from singpass
            if (HttpContext.Session.GetString("ApplyMethod") == "Singpass")
            {
                ac360.DateOfBirth = DateTime.Today;

                if (HttpContext.Session.GetString("Applicant") != "")
                {
                    foreach (Singpass sp in singpassContext.GetSingpassByNRIC(HttpContext.Session.GetString("Applicant")))
                    {
                        ac360.FullName = sp.Name;
                        ac360.NRIC = sp.NRIC;
                        ac360.DateOfBirth = sp.DoB;
                        ac360.CountryOfBirth = sp.CountryOfBirth;
                        ac360.Nationality = sp.Nationality;
                        if (sp.Gender == "M")
                        {
                            ac360.Gender = "Male";
                        }
                        else
                        {
                            ac360.Gender = "Female";
                        }
                        ac360.EmailAddress = sp.Email;
                        ac360.MobileNum = sp.MobileNum;
                        ac360.Address = sp.RegisteredAddress;
                        ac360.Employer = sp.EmployerName;
                    }
                    ac360.Address = ac360.Address.Trim();
                }
                return View(ac360);
            }  
            
            // Else if Scan run code to pull from Scan
            else if (HttpContext.Session.GetString("ApplyMethod") == "Scan")
            {
                // Get data from OCR
                Account360ViewModel OCRDetails = new Account360ViewModel();
                OCRDetails = HttpContext.Session.GetObjectFromJson<Account360ViewModel>("Scan");
                ac360.FullName = OCRDetails.FullName;
                if (OCRDetails.Gender == null)
                {
                    ac360.Gender = "";
                }
                else if (OCRDetails.Gender.Contains("F"))
                {
                    ac360.Gender = "Female";
                }
                else if (OCRDetails.Gender.Contains("M"))
                {
                    ac360.Gender = "Male";
                }
                ac360.CountryOfBirth = OCRDetails.CountryOfBirth;
                ac360.DateOfBirth = OCRDetails.DateOfBirth;
                ac360.NRIC = OCRDetails.NRIC;
                ac360.Address = OCRDetails.Address;
                if (OCRDetails.Nationality == "BRITISH")
                {
                    ac360.Nationality = "British, UK";
                }
                ac360.Address = ac360.Address.Trim();
                return View(ac360);
            }
            else // Else show some error
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(Account360ViewModel a360)
        {
            a360.Address = a360.Address.Trim();

            Account360ViewModel mainApplication = new Account360ViewModel();
            mainApplication.Salutation = a360.Salutation;
            mainApplication.FullName = a360.FullName;
            mainApplication.NRIC = a360.NRIC;
            mainApplication.DateOfBirth = a360.DateOfBirth;
            mainApplication.CountryOfBirth = a360.CountryOfBirth;
            mainApplication.Nationality = a360.Nationality;
            mainApplication.Gender = a360.Gender;
            mainApplication.MaritialStatus = a360.MaritialStatus;
            mainApplication.MobileNum = a360.MobileNum;
            mainApplication.EmailAddress = a360.EmailAddress;
            mainApplication.Address = a360.Address;
            mainApplication.Employer = a360.Employer;
            mainApplication.YearsInEmployment = a360.YearsInEmployment;
            mainApplication.Occupation = a360.Occupation;
            mainApplication.AnnualIncome = a360.AnnualIncome;
            mainApplication.SelfEmployeed = a360.SelfEmployeed;
            mainApplication.TaxResidence = "";

            bool haveError = false;

            // Validate NRIC
            string firstChar = a360.NRIC.Substring(0, 1);
            string lastChar = a360.NRIC.Substring(8,1);
            string middleNumbers = a360.NRIC.Substring(1, 7);
            if (firstChar.All(char.IsDigit) == true || lastChar.All(char.IsDigit) == true || middleNumbers.Any(x => char.IsLetter(x)) == true || mainApplication.NRIC.Count() != 9)
            {
                haveError = true;
                HttpContext.Session.SetString("NRIC_Error", "NRIC entered was invalid please enter details again!");          
            }

            // Validate Name
            if (Regex.IsMatch(mainApplication.FullName, @"^[A-Za-z\s]+$") == false)
            {
                HttpContext.Session.SetString("Name_Error", "Please enter only letters in your name!");
                haveError = true;
            }

            // Validate Age
            float age = 0;
            age = DateTime.Now.Subtract(mainApplication.DateOfBirth).Days;
            age /= 365;
            
            if (age <= 18 || age >= 110)
            {
                HttpContext.Session.SetString("Age_Error", "Please enter a valid date!");
                haveError = true;
            }

            if (haveError == true)
                return RedirectToAction("Form", "Account360");


            string temp = "";
            foreach (TaxResidency country in a360.TaxResidencyList)
            {
                if (country.Selected)
                {
                    temp += country.Country + ", ";
                }
            }
            temp = temp.Substring(0, temp.Length - 2);
            mainApplication.TaxResidence = temp;

            HttpContext.Session.SetObjectAsJson("ApplicantsDetails", mainApplication);

            if (HttpContext.Session.GetString("JAC") != null)
            {
                return RedirectToAction("Verify", "Account360");
            }
            return RedirectToAction("JointApplicant", "Account360");
        }

        /**==========================
                UPLOAD.CSHTML
        ==========================**/

        public ActionResult Upload()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("ApplyMethod", "Scan");

            return View("Upload");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(CustApplication custApplication)
        {
            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("ApplyMethod", "Scan");

            string uploadedNRICFront = "";
            string uploadedNRICBack = "";
            string uploadedResidentialProof = "";
            string savePath = "";

            if (custApplication.CustProofOfResidenceUpload != null && custApplication.CustProofOfResidenceUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustProofOfResidenceUpload.FileName);
                    uploadedResidentialProof = String.Format("residence_proof" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedResidentialProof);
                    HttpContext.Session.SetString("FilePathResidence", uploadedResidentialProof);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustProofOfResidenceUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            if (custApplication.CustNRICFrontUpload != null && custApplication.CustNRICFrontUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustNRICFrontUpload.FileName);
                    uploadedNRICFront = String.Format("nric_front" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedNRICFront);
                    HttpContext.Session.SetString("FilePathFront", uploadedNRICFront);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustNRICFrontUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            if (custApplication.CustNRICBackUpload != null && custApplication.CustNRICBackUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustNRICBackUpload.FileName);
                    uploadedNRICBack = String.Format("nric_back" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedNRICBack);
                    HttpContext.Session.SetString("FilePathBack", uploadedNRICBack);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustNRICBackUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            bool bypass = false;
            //save file in wwwrootfolder
            if (custApplication.SelfieURL != null)
            {
                try
                {
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\selfie.jpeg");
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        byte[] imgBytes = Convert.FromBase64String(custApplication.SelfieURL.Split(',')[1]);
                        fileSteam.Write(imgBytes);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            else
            {
                bypass = true;
            }
            string SourceUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedNRICFront);
            string TargetUrl = savePath;
            double confidence = 0;

            IFaceClient faceClient = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            try
            {
                double faceVerify = await FindSimilar(faceClient, SourceUrl, TargetUrl, RECOGNITION_MODEL4);
                confidence = faceVerify;
            }
            catch (IOException)
            {
                return View("Upload", custApplication);
            }
            catch (Exception)
            {
                if (!bypass)
                {
                    TempData["FaceError"] = "Something went wrong. Please try uploading your selfie again.";
                    return View("Upload", custApplication);
                }
            }
            if (confidence >= 0.4 || bypass == true)
            {
                // NRIC Front OCR API - DO NOT DELETE. COMMENTING OUT TO REDUCE API CALL USAGE.
                var client = new RestClient("https://app.nanonets.com/api/v2/OCR/Model/96fa0936-a5dd-4e70-96dd-0bae04e9d8f4/LabelFile/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("1xpmJsen1wvDnk5v-50NMcSF4uO5qCZp:")));
                request.AddHeader("accept", "Multipart/form-data");
                request.AddFile("file", ".\\wwwroot\\applicationdocs\\" + uploadedNRICFront);
                IRestResponse response = client.Execute(request);

                Account360ViewModel clientOCR = new Account360ViewModel();

                Dictionary<string, object> obj = (Dictionary<string, object>)OCBC_Online_Joint_Account.Models.JSONHelper.Deserialize(response.Content);
                foreach (var item in obj.Keys)
                {
                    if (item == "result")
                    {
                        List<object> results = (List<object>)obj[item];
                        Dictionary<string, object> predictions = (Dictionary<string, object>)results[0];
                        List<object> prediction = (List<object>)predictions["prediction"];
                        foreach (var p in prediction)
                        {
                            Dictionary<string, object> pvalue = (Dictionary<string, object>)p;
                            var label = (string)pvalue["label"];
                            var ocr_text = (string)pvalue["ocr_text"];
                            //Console.WriteLine("Label: " + label);
                            if (label == "NRIC")
                            {
                                clientOCR.NRIC = ocr_text;
                                clientOCR.NRIC = clientOCR.NRIC.Replace("O", "0");
                            }
                            else if (label == "Name")
                            {
                                clientOCR.FullName = ocr_text;
                            }
                            else if (label == "Sex")
                            {
                                clientOCR.Gender = ocr_text;
                            }
                            else if (label == "Date_of_Birth")
                            {
                                clientOCR.DateOfBirth = Convert.ToDateTime(ocr_text);
                            }
                            else if (label == "Country_Of_Birth")
                            {
                                clientOCR.CountryOfBirth = ocr_text;
                            }
                            //Console.WriteLine("Value: " + ocr_text);
                        }
                    }
                }

                // NRIC BACK OCR API - DO NOT DELETE. COMMENTING OUT TO REDUCE API CALL USAGE.
                var client2 = new RestClient("https://app.nanonets.com/api/v2/OCR/Model/8ee8790a-92db-48d7-adf0-c9512997b60a/LabelFile/");
                var request2 = new RestRequest(Method.POST);
                request2.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("_9xwEpzEdi3gevc7Oy-SucehAswdtRFG:")));
                request2.AddHeader("accept", "Multipart/form-data");
                request2.AddFile("file", ".\\wwwroot\\applicationdocs\\" + uploadedNRICBack);
                IRestResponse response2 = client2.Execute(request2);
                Dictionary<string, object> obj2 = (Dictionary<string, object>)OCBC_Online_Joint_Account.Models.JSONHelper.Deserialize(response2.Content);

                foreach (var item in obj2.Keys)
                {
                    if (item == "result")
                    {
                        List<object> results = (List<object>)obj2[item];
                        Dictionary<string, object> predictions = (Dictionary<string, object>)results[0];
                        List<object> prediction = (List<object>)predictions["prediction"];

                        foreach (var p in prediction)
                        {
                            Dictionary<string, object> pvalue = (Dictionary<string, object>)p;
                            var label = (string)pvalue["label"];
                            var ocr_text = (string)pvalue["ocr_text"];

                            if (label == "Address")
                            {
                                clientOCR.Address = ocr_text;
                            }

                            if (label == "NRIC_Back")
                            {
                                ocr_text = ocr_text.Replace("O ", "0");
                                if (ocr_text != clientOCR.NRIC)
                                {
                                    TempData["WrongIC"] = "Front and Back NRIC does not match, try again."; 
                                    return RedirectToAction("Upload");
                                }
                            }
                        }
                        break;
                    }
                }
                //Set clientOCR object to the "Scan" string to be used in Form.cshtml to parse the data from the OCR read
                HttpContext.Session.SetObjectAsJson("Scan", clientOCR);
                return RedirectToAction("Form");
            }
            else if (confidence == -1)
            {
                TempData["FaceError"] = "Something went wrong. Please try uploading your selfie again.";
                return View("Upload", custApplication);
            }
            else
            {
                TempData["FaceError"] = "Sorry but the faces in the pictures are not identical. Please try again.";
                return View("Upload", custApplication);
            }
        }

        /**==========================
            UploadForeign.CSHTML
        ==========================**/
        public ActionResult UploadForeign()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("ApplyMethod", "Scan");
            ViewData["IsSingaporean"] = true;
            return View("UploadForeign");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadForeign(CustApplication custApplication)
        {
            checkJAC(HttpContext.Session.GetString("JAC"));
            HttpContext.Session.SetString("ApplyMethod", "Scan");

            string uploadedForeignPassFront = "";
            string uploadedForeignPassBack = "";
            string uploadedResidentialProof = "";
            string uploadedPassport = "";
            string savePath = "";

            if (custApplication.CustProofOfResidenceUpload != null && custApplication.CustProofOfResidenceUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustProofOfResidenceUpload.FileName);
                    uploadedResidentialProof = String.Format("residence_proof" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedResidentialProof);
                    HttpContext.Session.SetString("FilePathResidence", uploadedResidentialProof);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustProofOfResidenceUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            if (custApplication.CustForeignPassFrontUpload != null && custApplication.CustForeignPassFrontUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustForeignPassFrontUpload.FileName);
                    uploadedForeignPassFront = String.Format("foreign_pass_front" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedForeignPassFront);
                    HttpContext.Session.SetString("FilePathFront", uploadedForeignPassFront);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustForeignPassFrontUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            if (custApplication.CustForeignPassBackUpload != null && custApplication.CustForeignPassBackUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustForeignPassBackUpload.FileName);
                    uploadedForeignPassBack = String.Format("foreign_pass_back" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedForeignPassBack);
                    HttpContext.Session.SetString("FilePathBack", uploadedForeignPassBack);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustForeignPassBackUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            if (custApplication.CustPassportUpload != null && custApplication.CustPassportUpload.Length > 0)
            {
                try
                {
                    string fileExt = Path.GetExtension(custApplication.CustPassportUpload.FileName);
                    uploadedPassport = String.Format("passport" + fileExt);
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedPassport);
                    HttpContext.Session.SetString("FilePathResidence", uploadedPassport);
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        await custApplication.CustPassportUpload.CopyToAsync(fileSteam);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            bool bypass = false;
            //save file in wwwrootfolder
            if (custApplication.SelfieURL != null)
            {
                try
                {
                    savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\selfie.jpeg");
                    using (var fileSteam = new FileStream(savePath, FileMode.Create))
                    {
                        byte[] imgBytes = Convert.FromBase64String(custApplication.SelfieURL.Split(',')[1]);
                        fileSteam.Write(imgBytes);
                    }
                }
                catch (IOException)
                {
                    return View("Upload", custApplication);
                }
                catch (Exception ex)
                {
                    ViewData["UploadMessage"] = ex.Message;
                    return View("Upload", custApplication);
                }
            }
            else
            {
                bypass = true;
            }
            string SourceUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\", uploadedForeignPassFront);
            string TargetUrl = savePath;
            double confidence = 0;

            IFaceClient faceClient = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            try
            {
                double faceVerify = await FindSimilar(faceClient, SourceUrl, TargetUrl, RECOGNITION_MODEL4);
                confidence = faceVerify;
            }
            catch (IOException)
            {
                return View("Upload", custApplication);
            }
            catch (Exception)
            {
                if (!bypass)
                {
                    TempData["FaceError"] = "Something went wrong. Please try uploading your selfie again.";
                    return View("Upload", custApplication);
                }
            }
            if (confidence >= 0.4 || bypass == true)
            {
                // NRIC Front OCR API - DO NOT DELETE. COMMENTING OUT TO REDUCE API CALL USAGE.
                var client = new RestClient("https://app.nanonets.com/api/v2/OCR/Model/96fa0936-a5dd-4e70-96dd-0bae04e9d8f4/LabelFile/");
                var request = new RestRequest(Method.POST);
                request.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("1xpmJsen1wvDnk5v-50NMcSF4uO5qCZp:")));
                request.AddHeader("accept", "Multipart/form-data");
                request.AddFile("file", ".\\wwwroot\\applicationdocs\\" + uploadedForeignPassFront);
                IRestResponse response = client.Execute(request);

                Account360ViewModel clientOCR = new Account360ViewModel();

                Dictionary<string, object> obj = (Dictionary<string, object>)OCBC_Online_Joint_Account.Models.JSONHelper.Deserialize(response.Content);
                foreach (var item in obj.Keys)
                {
                    if (item == "result")
                    {
                        List<object> results = (List<object>)obj[item];
                        Dictionary<string, object> predictions = (Dictionary<string, object>)results[0];
                        List<object> prediction = (List<object>)predictions["prediction"];
                        foreach (var p in prediction)
                        {
                            Dictionary<string, object> pvalue = (Dictionary<string, object>)p;
                            var label = (string)pvalue["label"];
                            var ocr_text = (string)pvalue["ocr_text"];
                            //Console.WriteLine("Label: " + label);
                            if (label == "NRIC")
                            {
                                clientOCR.NRIC = ocr_text;
                            }
                            else if (label == "Name")
                            {
                                clientOCR.FullName = ocr_text;
                            }
                            else if (label == "Sex")
                            {
                                clientOCR.Gender = ocr_text;
                            }
                            else if (label == "Date_of_Birth")
                            {
                                clientOCR.DateOfBirth = Convert.ToDateTime(ocr_text);
                            }
                            else if (label == "Country_Of_Birth")
                            {
                                clientOCR.CountryOfBirth = ocr_text;
                            }
                            //Console.WriteLine("Value: " + ocr_text);
                        }
                    }
                }

                // NRIC BACK OCR API - DO NOT DELETE. COMMENTING OUT TO REDUCE API CALL USAGE.
                var client2 = new RestClient("https://app.nanonets.com/api/v2/OCR/Model/8ee8790a-92db-48d7-adf0-c9512997b60a/LabelFile/");
                var request2 = new RestRequest(Method.POST);
                request2.AddHeader("authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("_9xwEpzEdi3gevc7Oy-SucehAswdtRFG:")));
                request2.AddHeader("accept", "Multipart/form-data");
                request2.AddFile("file", ".\\wwwroot\\applicationdocs\\" + uploadedForeignPassFront);
                IRestResponse response2 = client2.Execute(request2);
                Dictionary<string, object> obj2 = (Dictionary<string, object>)OCBC_Online_Joint_Account.Models.JSONHelper.Deserialize(response2.Content);

                foreach (var item in obj2.Keys)
                {
                    if (item == "result")
                    {
                        List<object> results = (List<object>)obj2[item];
                        Dictionary<string, object> predictions = (Dictionary<string, object>)results[0];
                        List<object> prediction = (List<object>)predictions["prediction"];

                        foreach (var p in prediction)
                        {
                            Dictionary<string, object> pvalue = (Dictionary<string, object>)p;
                            clientOCR.Address = (string)pvalue["ocr_text"];
                            break;
                        }
                        break;
                    }
                }
                ////Set clientOCR object to the "Scan" string to be used in Form.cshtml to parse the data from the OCR read
                HttpContext.Session.SetObjectAsJson("Scan", clientOCR);
                return RedirectToAction("Form");
            }
            else if (confidence == -1)
            {
                TempData["FaceError"] = "Something went wrong. Please try uploading your selfie again.";
                return View("Upload", custApplication);
            }
            else
            {
                TempData["FaceError"] = "Sorry but the faces in the pictures are not identical. Please try again.";
                return View("Upload", custApplication);
            }
        }

        /**==========================
            JOINTAPPLICANT.CSHTML
        ==========================**/

        public ActionResult JointApplicant()
        {
            HttpContext.Session.SetString("PageType", "Account360");

            // Second Applicant
            if ((HttpContext.Session.GetString("JAC") != null || HttpContext.Session.GetString("ContinueWifi") != null) && (HttpContext.Session.GetString("ApplyMethod") == "QR" || HttpContext.Session.GetString("ApplyMethod") == "iBanking" || HttpContext.Session.GetString("CustSingpass") == "existingCustomer"))
            {
                checkJAC(HttpContext.Session.GetString("JAC"));
                Account360ViewModel ac360 = new Account360ViewModel();
                if (customerContext.GetCustomerByNRIC(HttpContext.Session.GetString("iBankingLogin")) != null)
                {
                    foreach (Customer c in customerContext.GetCustomerByNRIC(HttpContext.Session.GetString("iBankingLogin")))
                    {
                        ac360.NRIC = c.CustNRIC;
                        ac360.Salutation = c.Salutation;
                        ac360.FullName = c.CustName;
                        ac360.EmailAddress = c.Email;
                        ac360.MobileNum = c.ContactNo;
                        if (c.Gender == "M")
                        {
                            ac360.Gender = "Male";
                        }
                        else
                        {
                            ac360.Gender = "Female";
                        }
                        ac360.MaritialStatus = c.MaritialStatus;
                        ac360.Address = c.Address;
                        ac360.CountryOfBirth = c.CountryOfBirth;
                        ac360.Nationality = c.Nationality;
                        ac360.DateOfBirth = c.DateOfBirth;
                        ac360.Employer = c.EmployerName;
                        ac360.Occupation = c.Occupation;
                        ac360.AnnualIncome = c.Income;
                    }
                }

                if (HttpContext.Session.GetString("ApplyMethod") == "QR")
                {
                    foreach (Customer c in customerContext.GetCustomerByNRIC(HttpContext.Session.GetString("MainApplicantNRIC")))
                    {
                        ac360.SalutationJoint = c.Salutation;
                        ac360.JointApplicantName = c.CustName;
                        ac360.Email = c.Email;
                        ac360.JointApplicantNRIC = c.CustNRIC;
                        ac360.ContactNo = c.ContactNo;
                    }
                }

                HttpContext.Session.SetObjectAsJson("ApplicantsDetails", ac360);
                return RedirectToAction("Verify", "Account360");
            }
            
            ResetQR();
            HttpContext.Session.SetString("PageType", "Account360");
            ViewData["Salutation"] = Salutation;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult JointApplicant(Account360ViewModel a360)
        {
            Account360ViewModel ac360 = new Account360ViewModel();

            if (HttpContext.Session.GetString("ApplyMethod") == "QR" || HttpContext.Session.GetString("ApplyMethod") == "iBanking" || HttpContext.Session.GetString("CustSingpass") == "existingCustomer")
            {
                /*TempData*/ViewData["CustSingass"] = "existingCustomer";
                foreach (Customer c in customerContext.GetCustomerByNRIC(HttpContext.Session.GetString("iBankingLogin")))
                {
                    ac360.NRIC = c.CustNRIC;
                    ac360.Salutation = c.Salutation;
                    ac360.FullName = c.CustName;
                    ac360.EmailAddress = c.Email;
                    ac360.MobileNum = c.ContactNo;
                    ac360.Gender = c.Gender;
                    ac360.MaritialStatus = c.MaritialStatus;
                    ac360.Address = c.Address;
                    ac360.CountryOfBirth = c.CountryOfBirth;
                    ac360.Nationality = c.Nationality;
                    ac360.DateOfBirth = c.DateOfBirth;
                    ac360.Employer = c.EmployerName;
                    ac360.Occupation = c.Occupation;
                    ac360.AnnualIncome = c.Income;
                }

                HttpContext.Session.SetObjectAsJson("ApplicantsDetails", ac360);
            }

            ac360 = HttpContext.Session.GetObjectFromJson<Account360ViewModel>("ApplicantsDetails");
            ac360.SalutationJoint = a360.SalutationJoint;
            ac360.JointApplicantName = a360.JointApplicantName;
            ac360.Email = a360.Email;
            ac360.ContactNo = a360.ContactNo;
            ac360.JointApplicantNRIC = a360.JointApplicantNRIC;
            HttpContext.Session.SetObjectAsJson("ApplicantsDetails", ac360);

            return RedirectToAction("Verify", "Account360");
        }

        /**==========================
               VERIFY.CSHTML
       ==========================**/
        public ActionResult Verify()
        {
            ResetQR();
            HttpContext.Session.SetString("PageType", "Account360");
            checkJAC(HttpContext.Session.GetString("JAC")); // Check Main or Joint       

            Account360ViewModel ac360 = new Account360ViewModel();
            ac360 = HttpContext.Session.GetObjectFromJson<Account360ViewModel>("ApplicantsDetails");
            if (ac360.Gender == "M" || ac360.Gender == "Male")
                ac360.Gender = "Male";
            else
                ac360.Gender = "Female";

            if (HttpContext.Session.GetString("ApplyMethod") != "QR" && HttpContext.Session.GetString("ApplyMethod") != "iBanking" && HttpContext.Session.GetString("CustSingpass") != "existingCustomer" )
            {
                ac360.Occupation = Occupation[(Convert.ToInt32(ac360.Occupation) - 1)].Text;
                ac360.AnnualIncome = AnnualIncome[(Convert.ToInt32(ac360.AnnualIncome) - 1)].Text;
            }

            ViewData["DateOfBirth"] = ac360.DateOfBirth.Date.ToString("d");

            if (HttpContext.Session.GetString("JAC") != null)
            {
                List<Application> mainApplication = applicationContext.GetApplicationByJointApplicantionCode(HttpContext.Session.GetString("JAC"));
                // Setting applicationID with JAC
                foreach (Application a in mainApplication)
                {
                    foreach (Customer c in customerContext.GetCustomerByNRIC(a.CustNRIC))
                    {
                        ac360.SalutationJoint = c.Salutation;
                        ac360.JointApplicantName = c.CustName;
                        ac360.JointApplicantNRIC = c.CustNRIC;
                        ac360.Email = c.Email;
                        ac360.ContactNo = c.ContactNo;
                    }
                }
            }
            else if (HttpContext.Session.GetString("ContinueWifi") != null && (HttpContext.Session.GetString("ApplyMethod") == "QR" || HttpContext.Session.GetString("ApplyMethod") == "iBanking" || HttpContext.Session.GetString("CustSingpass") == "existingCustomer"))
            {
                string phrase = HttpContext.Session.GetString("ContinueWifi");
                string[] words = phrase.Split(',');

                ac360.SalutationJoint = words[0];
                ac360.JointApplicantName = words[1];
                ac360.JointApplicantNRIC = words[2];
                ac360.Email = words[9];
                ac360.ContactNo = words[10];

                //ac360.NRIC = words[2];
                //ac360.Salutation = words[0];
                //ac360.FullName = words[1];
                //ac360.EmailAddress = words[9];
                //ac360.MobileNum = words[10];
                //if (words[7] == "M")
                //{
                //    ac360.Gender = "Male";
                //}
                //else
                //{
                //    ac360.Gender = "Female";
                //}
                //ac360.MaritialStatus = words[6];
                //ac360.Address = words[8];
                //ac360.CountryOfBirth = words[4];
                //ac360.Nationality = words[5];
                //ac360.DateOfBirth = Convert.ToDateTime(words[3]);
                //ac360.Employer = words[13];
                //ac360.Occupation = words[11];
                //ac360.AnnualIncome = words[12];
            }

            HttpContext.Session.SetObjectAsJson("ApplicantsDetails", ac360);
            return View(ac360);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Verify(Account360ViewModel a360, int? id)
        {
            
            if(id == 1)
            {               
                string JointAC = HttpContext.Session.GetString("JAC");

                // This line of code to purely get the date of birth because it does not exisit in the post page of verify
                Account360ViewModel ac360 = new Account360ViewModel();
                ac360 = HttpContext.Session.GetObjectFromJson<Account360ViewModel>("ApplicantsDetails");

                // Only scan and new singpass is add, rest update
                // Convert gender to single char
                if (ac360.Gender == "Male" || ac360.Gender == "M")
                {
                    ac360.Gender = "M";
                }
                else
                {
                    ac360.Gender = "F";
                }

                // Add application table
                Application newApplication = new Application();

                // Create CustApplication
                CustApplication custApp = new CustApplication();

                //Application table stuff
                newApplication.CustNRIC = a360.NRIC;
                newApplication.AccountTypeID = (int)HttpContext.Session.GetInt32("AccountTypeID");

                //custApplication table stuff
                custApp.CustNRIC = a360.NRIC;

                custApp.CustProofOfResidence = HttpContext.Session.GetString("FilePathResidence");
                // Check if foreign
                if (Convert.ToBoolean(ViewData["IsSingaporean"]) == true)
                {
                    custApp.CustNRICFront = HttpContext.Session.GetString("FilePathFront");
                    custApp.CustNRICBack = HttpContext.Session.GetString("FilePathBack");
                }
                else
                {
                    custApp.CustForeignPassFront = HttpContext.Session.GetString("FilePathFront");
                    custApp.CustForeignPassBack = HttpContext.Session.GetString("FilePathBack");
                    custApp.CustPassport = HttpContext.Session.GetString("FilePathPassport");
                }

                // Add to customer table
                if (HttpContext.Session.GetString("ApplyMethod") == "Scan" || HttpContext.Session.GetString("CustSingpass") == "newCustomer")
                {
                    customerContext.Add(ac360);
                }

                newApplication.Status = "Pending";
               
                // Main applicant
                if (JointAC == null || HttpContext.Session.GetString("ContinueWifi") != null)
                {
                    Random rnd = new Random();
                    int rndNum1 = rnd.Next(100000000, 999999999);
                    string JAC = "J" + DateTime.Today.Day + rndNum1 + a360.NRIC.Substring(5, 3);
                    HttpContext.Session.SetString("JACWIFI", JAC);
                    TempData["JAC"] = JAC;

                    if (HttpContext.Session.GetString("ContinueWifi") != null)
                    {
                        newApplication.Status = "Successful";
                        HttpContext.Session.SetString("JAC", JAC);
                        RunAsyncFinal(a360.Salutation, a360.FullName, a360.Email, JAC).Wait();
                    }

                    if(HttpContext.Session.GetString("ContinueWifi") == null)
                    {
                        // Email API
                        RunAsync(a360.Salutation, a360.FullName, a360.Email, JAC, a360.SalutationJoint, a360.JointApplicantName).Wait();

                        //Send Unique Link via SMS
                        try
                        {
                            var accountSid = "AC900a65cf35b142ba9d231968f7975595";
                            var authToken = "6dbf2032023e857f59f960615d9afb65";
                            TwilioClient.Init(accountSid, authToken);
                            var messageOptions = new CreateMessageOptions(new PhoneNumber("+65" + a360.ContactNo));
                            messageOptions.MessagingServiceSid = "MG9dc1a6ffbac9048864eaadfda51637fc";
                            messageOptions.Body = "OCBC: 360 Account Joint-Application\n\nDear " + a360.JointApplicantName + "\n\nMr " + a360.FullName + " has initiated a Joint-Account application and is requesting you to complete it.\nYou may complete your application via https://ocbc-npt2.azurewebsites.net/Account360/ApplyOnline?AT=2&JAC=" + JAC + "\n\nIf you don't know this person, call 1800 363 333 at once.";
                            var message = MessageResource.Create(messageOptions);
                            Console.WriteLine(message.Body);
                        }
                        catch (Twilio.Exceptions.ApiException)
                        {
                            return RedirectToAction("Index", "Home");
                        }

                        Console.WriteLine(" https://localhost:44381/Account360/ApplyOnline?AT=2&JAC=" + JAC);
                    }
          
                    newApplication.JointApplicantCode = JAC;
                    custApp.JointApplicantName = a360.JointApplicantName;
                    custApp.JointApplicantNRIC = a360.JointApplicantNRIC;
                    applicationContext.Add(newApplication);
                }
                
                if (JointAC != null) // Joint applicant
                {
                    List<Application> mainApplication = mainApplication = applicationContext.GetApplicationByJointApplicantionCode(JointAC);
                       
                    // Setting applicationID with JAC
                    foreach (Application a in mainApplication)
                    {
                        newApplication.JointApplicantID = a.ApplicationID;
                        a.JointApplicantID = applicationContext.Add(newApplication);
                        custApp.ApplicationID = (int)a.JointApplicantID;
                        applicationContext.Update(a);

                        // Getting iUsername of main applicant                                               // Getting iUsername of joint applicant
                        if (applicationContext.GetiUsername((int)newApplication.JointApplicantID) != null && applicationContext.GetiUsername((int)a.JointApplicantID) != null)
                        {
                            newApplication.Status = "Successful";
                            a.Status = "Successful";
                            applicationContext.Update(a);
                            applicationContext.Update(newApplication);

                            //Create the bank account
                            BankAccount bAcc = new BankAccount();
                            bAcc.Balance = 0;
                            bAcc.AccountTypeID = (int)HttpContext.Session.GetInt32("AccountTypeID");
                            Random rnd = new Random();
                            int rand1 = rnd.Next(0, 9);
                            int rand2 = rnd.Next(100000, 999999);
                            int rand3 = rnd.Next(10000, 99999);
                            string accNo = rand1.ToString() + rand2.ToString() + rand3.ToString();
                            bAcc.AccountNo = accNo;

                            // Add to bank account
                            bankContext.Add(bAcc);

                            // Main applicant
                            CustomerAccounts mainCustAcc = new CustomerAccounts();
                            mainCustAcc.CustNRIC = ac360.JointApplicantNRIC;
                            mainCustAcc.AccountNo = accNo;
                            custAccContext.Add(mainCustAcc);

                            // Joint applicant
                            CustomerAccounts jointCustAcc = new CustomerAccounts();
                            jointCustAcc.CustNRIC = ac360.NRIC;
                            jointCustAcc.AccountNo = accNo;
                            custAccContext.Add(jointCustAcc);

                            RunAsyncFinal(a360.Salutation, a360.FullName, a360.Email, JointAC).Wait();
                        }
                        else
                        {
                            newApplication.Status = "To Review";
                            a.Status = "To Review";
                            applicationContext.Update(a);
                            applicationContext.Update(newApplication);
                        }
                    }
                }


                // To add the application ID for custApplication
                if (JointAC == null)
                {
                    string JAC = Convert.ToString(TempData["JAC"]);
                    List<Application> mainApplication = applicationContext.GetApplicationByJointApplicantionCode(JAC);
                    // Setting applicationID with JAC
                    foreach (Application a in mainApplication)
                    {
                        custApp.ApplicationID = a.ApplicationID;
                    }
                }

                custApplicationContext.Add(custApp);
                return RedirectToAction("Success", "Account360");
            }
            else if(id == 2) // Continue with WIFI
            {
                string data = a360.Salutation + "," + a360.FullName + "," + a360.NRIC + "," + a360.DateOfBirth + "," + a360.CountryOfBirth + "," + a360.Nationality + "," + a360.MaritialStatus + "," + a360.Gender + "," + a360.Address + "," + a360.EmailAddress + "," + a360.MobileNum + "," + a360.Occupation + "," + a360.AnnualIncome + "," + a360.Employer + "," + a360.SalutationJoint + "," + a360.JointApplicantName + "," + a360.JointApplicantNRIC + "," + a360.Email + "," + a360.ContactNo;
                HttpContext.Session.SetString("data", data);
   
                return RedirectToAction("Network", "Account360");
            }
            return View();
        }

        public ActionResult Success()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            Account360ViewModel ac360 = new Account360ViewModel();
            ac360 = HttpContext.Session.GetObjectFromJson<Account360ViewModel>("ApplicantsDetails");
            if (HttpContext.Session.GetString("JAC") != null)
            {
                foreach (Application a in applicationContext.GetApplicationByJointApplicantionCode(HttpContext.Session.GetString("JAC")))
                {
                    if (a.Status == "Successful")
                    {
                        ViewData["Status"] = "Successful";
                    }
                    else if (a.Status == "To Review")
                    {
                        ViewData["Status"] = "To Review";
                    }
                }
                ViewData["Time"] = DateTime.Now;
            }
            ViewData["Name"] = ac360.JointApplicantName;
            ViewData["Email"] = ac360.Email;
            ViewData["ContactNo"] = ac360.ContactNo;
            return View();
        }

        public ActionResult ContinueOnMobile()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            return View();
        }

        public ActionResult Denied()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            return View();
        }

       
        private static string GetMachineNameFromIPAddress(string ipAdress)
        {
            string machineName = string.Empty;
            try
            {
                IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(ipAdress);
                machineName = hostEntry.HostName;
            }
            catch (Exception ex)
            {
                machineName = "No Hostname Found";
            }
            return machineName;
        }

        public ActionResult WIFI()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            string hostname = GetMachineNameFromIPAddress(IPAddress.Loopback.ToString());
            ViewData["Hostname"] = hostname;


            //Start Broadcasting
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("192.168.1.255");
            byte[] sendbuf = Encoding.ASCII.GetBytes(hostname);
            IPEndPoint ep = new IPEndPoint(broadcast, 11000);
            s.SendTo(sendbuf, ep);

            var server = new SimpleTcpServer();
            int clientsConnected = server.ConnectedClientsCount;
            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(IPAddress.Loopback, 8910);
                }
                catch (Exception)
                {
                    server.Start(8910);
                }
            }

            server.Delimiter = 0x13;
            server.DelimiterDataReceived += (sender, msg) => {
                var path = "data.txt";
                using var sw = new StreamWriter(path);
                sw.WriteLine(msg.MessageString);
                Console.WriteLine(msg.MessageString);
            };

            if(System.IO.File.ReadAllText("data.txt") != "Empty")
            {
                Console.WriteLine(System.IO.File.ReadAllText("data.txt"));
                return RedirectToAction("Connect", "Account360");
            }

            return View();
        }

        public ActionResult Connect()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            return View();
        }

        [HttpPost]
        public ActionResult Connect(int? id)
        {
            HttpContext.Session.SetString("PageType", "Account360");
            if (id == 1)
            {
                HttpContext.Session.SetString("data", "");
                return RedirectToAction("WIFI", "Account360");
            }
            else if(id == 2)
            {
                HttpContext.Session.SetString("ContinueWifi", System.IO.File.ReadAllText("data.txt"));
                return RedirectToAction("ApplyOnline", "Account360");
            }
            return View();
        }

        public ActionResult Network()
        {
            HttpContext.Session.SetString("PageType", "Account360");

            // Start Listening
            UdpClient listener = new UdpClient(11000);
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, 11000);
            try
            {
                Console.WriteLine("Waiting for broadcast");
                byte[] bytes = listener.Receive(ref groupEP);
                HttpContext.Session.SetString("ListeningIPAddress", groupEP.Address.ToString());
                HttpContext.Session.SetString("ListeningHostname", Encoding.ASCII.GetString(bytes, 0, bytes.Length));
                Console.Write($"Received broadcast from {groupEP.Address}");
                Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }


            ViewData["TCPIp"] = groupEP.Address.ToString();
            ViewData["TCPHost"] = HttpContext.Session.GetString("ListeningHostname");
            return View();
        }

        [HttpPost]
        public ActionResult Network(int? id)
        {
            HttpContext.Session.SetString("PageType", "Account360");
            if(id == 1)
            {
                return RedirectToAction("Verify", "Account360");
            }
            else if(id == 2)
            {
                //Establish using SimpleTCP - Client Side
                var client = new SimpleTcpClient().Connect(HttpContext.Session.GetString("ListeningIPAddress"), 8910);
                var replyMsg = client.WriteLineAndGetReply(HttpContext.Session.GetString("data"), TimeSpan.FromSeconds(3));
                client.DelimiterDataReceived += (sender, msg) =>
                {
                    Console.WriteLine(msg.MessageString);
                };
                return RedirectToAction("SuccessWIFI", "Account360");
            }   
            return View();
        }

        public ActionResult SuccessWIFI()
        {
            HttpContext.Session.SetString("PageType", "Account360");
            return View();
        }
        /**==========================
                    METHODS
        ==========================**/
        static async Task RunAsyncFinal(string sal, string name, string email, string jac)
        {
            MailjetClient client = new MailjetClient("15d7f5da982f1c3ef8e03b2f27ef7b2a", "adc898336379f915ee832b698356ab3f");
            MailjetRequest request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
               .Property(Send.FromEmail, "s10208193@connect.np.edu.sg")
               .Property(Send.FromName, "OCBC Bank")
               .Property(Send.Subject, "360 Account Joint-Account Application Completed")
               .Property(Send.TextPart, "")
               .Property(Send.HtmlPart, "<div style='text-align: center; margin: 0 20% 0 20%'><img style='height: 150px' src='https://1000logos.net/wp-content/uploads/2021/09/OCBC-Bank-Logo.png'><h1><b>360 Account Joint-Account Application</b></h1><hr><div style='text-align: left; font-weight: lighter;'><h3 style='font-weight: lighter; margin-top: 40px'>Dear " + sal + " " + name + "</h3><h3 style='font-weight: lighter; margin-top: 40px'> Your application for 360 Joint-Account Application is currently pending. The reference code for the application is " + jac + ".</h3><h3 style='font-weight: lighter; margin-top: 40px'>If you do not know this person, call 1800 363 333 at once.</h3><h3 style='font-weight: lighter; margin-top: 40px'>Yours sincerely<br><b>OCBC Bank</b></h3></div></div>")
               .Property(Send.Recipients, new JArray { new JObject { { "Email", email } } });
            MailjetResponse response = await client.PostAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(string.Format("Total: {0}, Count: {1}\n", response.GetTotal(), response.GetCount()));
                Console.WriteLine(response.GetData());
            }
            else
            {
                Console.WriteLine(string.Format("StatusCode: {0}\n", response.StatusCode));
                Console.WriteLine(string.Format("ErrorInfo: {0}\n", response.GetErrorInfo()));
                Console.WriteLine(string.Format("ErrorMessage: {0}\n", response.GetErrorMessage()));
                Console.ReadLine();
            }
        }

        static async Task RunAsync(string sal, string name, string email, string jac, string sj, string jan)
        {
            MailjetClient client = new MailjetClient("15d7f5da982f1c3ef8e03b2f27ef7b2a", "adc898336379f915ee832b698356ab3f");
            MailjetRequest request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
               .Property(Send.FromEmail, "s10208193@connect.np.edu.sg")
               .Property(Send.FromName, "OCBC Bank")
               .Property(Send.Subject, "360 Account Joint-Account Application")
               .Property(Send.TextPart, "")
               .Property(Send.HtmlPart, "<div style='text-align: center; margin: 0 20% 0 20%'><img style='height: 150px' src='https://1000logos.net/wp-content/uploads/2021/09/OCBC-Bank-Logo.png'><h1><b>360 Account Joint-Account Application</b></h1><hr><div style='text-align: left; font-weight: lighter;'><h3 style='font-weight: lighter; margin-top: 40px'>Dear " + sj + " " + jan + "</h3><h3 style='font-weight: lighter; margin-top: 40px'>" + sal + " " + name + " has initiated a Joint-Account application and is requesting you to complete it. Simply click on this <a href='https://ocbc-npt2.azurewebsites.net/Account360/ApplyOnline?AT=2&JAC=" + jac + "'>link</a> to complete your application.</h3><h3 style='font-weight: lighter; margin-top: 40px'>If you do not know this person, call 1800 363 333 at once.</h3><h3 style='font-weight: lighter; margin-top: 40px'>Yours sincerely<br><b>OCBC Bank</b></h3></div></div>")
               .Property(Send.Recipients, new JArray { new JObject { { "Email", email } } });
            MailjetResponse response = await client.PostAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(string.Format("Total: {0}, Count: {1}\n", response.GetTotal(), response.GetCount()));
                Console.WriteLine(response.GetData());
            }
            else
            {
                Console.WriteLine(string.Format("StatusCode: {0}\n", response.StatusCode));
                Console.WriteLine(string.Format("ErrorInfo: {0}\n", response.GetErrorInfo()));
                Console.WriteLine(string.Format("ErrorMessage: {0}\n", response.GetErrorMessage()));
                Console.ReadLine();
            }
        }

        public bool isCorrectApplicantLogin()
        {    
            if (HttpContext.Session.GetString("JAC") != null)
            {
                foreach (Application a in applicationContext.GetApplicationByJointApplicantionCode(HttpContext.Session.GetString("JAC")))
                {
                    foreach(CustApplication ca in custApplicationContext.GetCustApplicationByNRIC(a.CustNRIC))
                    {
                        if (HttpContext.Session.GetString("Applicant") == ca.JointApplicantNRIC)
                        {
                            return true;
                        }
                    }          
                    return false;
                }  
            }
            return true;
        }

        public void checkJAC(string JAC)
        {
            if (HttpContext.Session.GetString("JAC") != null)
            {
                foreach (Application a in applicationContext.GetApplicationByJointApplicantionCode(JAC))
                {
                    
                    foreach (Customer c in customerContext.GetCustomerByNRIC(a.CustNRIC))
                    {
                        ViewData["MainSalutation"] = c.Salutation;
                        ViewData["MainName"] = c.CustName;
                    }                 
                }            
            }
        }

        public void ResetQR()
        {
            //QR: Reset QR settings
            var resetQR =
                "{\"qr_data\":\"ocbc_jointacc_digital_create\"," +
                 "\"custNRIC\":null," +
                "\"hasScanned\":false," +
                "\"toRedirect\":false," +
                "\"continueMobile\":false," +
                "\"isJointApplicant\":false," +
                "\"selectedAccountTypeId\":2," +
                "\"selectedAccountTypeName\":\"360 Account\"," +
                "\"mainApplicantName\":null," +
                "\"mainApplicantNRIC\":null," +
                "\"jointApplicationCode\":null," +
                "\"id\":0}";

            var client = new RestClient("https://pfdocbcdb-5763.restdb.io/rest/qr-response/6191214a9402c24f00017a99");
            var request = new RestRequest(Method.PUT);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("x-apikey", "f3e68097c1a4127f4472d8730dcb3399f2d14");
            request.AddHeader("content-type", "application/json");
            request.AddParameter("application/json", resetQR, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
        }
        public void InsertQRForJointApplicant(int? AT, string JAC)
        {
            //QR: Reset QR settings
            string Name = "";
            string NRIC = "";

            foreach (Application a in applicationContext.GetApplicationByJointApplicantionCode(JAC))
            {
                foreach (Customer c in customerContext.GetCustomerByNRIC(a.CustNRIC))
                {
                    Name = c.CustName;
                    NRIC = a.CustNRIC;
                }
            }

            var resetQR =
                "{\"qr_data\":\"ocbc_jointacc_digital_create\"," +
                "\"custNRIC\":null," +
                "\"hasScanned\":false," +
                "\"toRedirect\":false," +
                "\"continueMobile\":false," +
                "\"isJointApplicant\":true," +
                "\"selectedAccountTypeId\":" + AT + "," +
                "\"selectedAccountTypeName\":\"360 Account\"," +
                "\"mainApplicantName\":\"" + Name + "\"," +
                "\"mainApplicantNRIC\":\"" + NRIC + "\"," +
                "\"jointApplicationCode\":\"" + JAC + "\"," +
                "\"id\":0}";

            var client1 = new RestClient("https://pfdocbcdb-5763.restdb.io/rest/qr-response/6191214a9402c24f00017a99");
            var request1 = new RestRequest(Method.PUT);
            request1.AddHeader("cache-control", "no-cache");
            request1.AddHeader("x-apikey", "f3e68097c1a4127f4472d8730dcb3399f2d14");
            request1.AddHeader("content-type", "application/json");
            request1.AddParameter("application/json", resetQR, ParameterType.RequestBody);
            IRestResponse response = client1.Execute(request1);
        }
        public string ResponseQR()
        {
            //QR: Wait for response from iBanking App
            string conti = null;
            var client = new RestClient("https://pfdocbcdb-5763.restdb.io/rest/qr-response/6191214a9402c24f00017a99");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("x-apikey", "f3e68097c1a4127f4472d8730dcb3399f2d14");
            request.AddHeader("content-type", "application/json");
            IRestResponse response = client.Execute(request);
            QR qr = JsonConvert.DeserializeObject<QR>(response.Content);

            if (qr.hasScanned == true && qr.toRedirect == true && qr.continueMobile == false)
            {
                if (qr.custNRIC != null)
                {
                    HttpContext.Session.SetString("ApplyMethod", "QR");
                    HttpContext.Session.SetString("iBankingLogin", qr.custNRIC);
                    HttpContext.Session.SetString("MainApplicantNRIC", qr.mainApplicantNRIC);
                }
                conti = "ContinueOnWeb";
            }
            else if(qr.hasScanned == true && qr.toRedirect == false && qr.continueMobile == true)
            {
                conti = "ContinueOnMobile";
            }
            return conti;
        }
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            try
            {
                StreamReader s = new StreamReader(url);
                Stream img = s.BaseStream;
                IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(img, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03);
                return detectedFaces.ToList();
            }
            catch
            {
                return null;
            }
            
        }
        /*
       * FIND SIMILAR
       * This example will take an image and find a similar one to it in another image.
       */
        public static async Task<double> FindSimilar(IFaceClient client, string SourceUrl, string TargetUrl, string recognition_model)
        {
            string targetImageFileName = TargetUrl;
            string sourceImageFileName = SourceUrl;

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, sourceImageFileName, recognition_model);
            if (detectedFaces.Count > 2)
            {
                return -1;
            }

            // Detect faces from target image url.
            var faces = await DetectFaceRecognize(client, targetImageFileName, recognition_model);
            if (faces.Count > 1)
            {
                return -1;
            }
            
            Guid NRICface1 = detectedFaces[0].FaceId.Value;
            Guid faceId = faces[0].FaceId.Value;
            VerifyResult result1 = await client.Face.VerifyFaceToFaceAsync(faceId, NRICface1);
            double bestResult = result1.Confidence;
            if (detectedFaces.Count == 2)
            {
                Guid NRICface2 = detectedFaces[1].FaceId.Value;
                VerifyResult result2 = await client.Face.VerifyFaceToFaceAsync(faceId, NRICface2);
                bestResult = Math.Max(result1.Confidence, result2.Confidence);
            }
            return bestResult;
        }
        //Authenticate Face client
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
        [HttpPost]
        public static bool SaveCapturedImage(string data)
        {
            //Convert Base64 Encoded string to Byte Array.
            byte[] imageBytes = Convert.FromBase64String(data.Split(',')[1]);

            //save location
            string savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\applicationdocs\\selfie.jpeg");



            ////Save the Byte Array as Image File.
            //using (var ms = new MemoryStream(imageBytes))
            //{
            //    using (var fileStream = new FileStream(savePath, FileMode.Create))
            //    {
            //        ms.WriteTo(fileStream);
            //    }
            //}

            return true;
        }
    }
}

