using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using RestSharp;
using OCBC_Joint_Account_Application.Models;
using OCBC_Joint_Account_Application.DAL;
using System.IO;
using System.Net;

namespace OCBC_Joint_Account_Application.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDAL applicationContext = new ApplicationDAL();
        private readonly ILogger<HomeController> _logger;  

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public ActionResult Index()
        {
            HttpContext.Session.SetString("PageType", "OCBC");
            HttpContext.Session.SetString("Applicant", "");
            ResetQR();

            var path = "data.txt";
            using var sw = new StreamWriter(path);
            sw.Write("Empty");

            if (HttpContext.Session.GetString("JAC") != null)
            {
                HttpContext.Session.Remove("JAC");
            };
            return View();      
        }

        [HttpPost]
        public ActionResult Index(HomeViewModel hm)
        {
            return RedirectToAction("ApplicationStatus", "Home", new { JAC = hm.ApplicationStatus });
        }

        public ActionResult ApplicationStatus(string? JAC)
        {
            HttpContext.Session.SetString("PageType", "Account360");
            foreach (Application hm in applicationContext.GetApplicationByJointApplicantionCode(JAC))
            {
                if(hm.JointApplicantCode == JAC)
                {
                    ViewData["AS"] = hm.Status;
                }
            }
            return View();
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
    }
}
