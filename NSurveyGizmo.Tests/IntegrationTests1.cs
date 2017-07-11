using NSurveyGizmo;
using NSurveyGizmo.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NSurveyGizmo.Tests
{
    public partial class IntegrationTests
    {
        private DateTime testStartedAt;
        private ApiClient apiClient;

        [TestInitialize]
        public void Initialize()
        {
            var creds = File.ReadAllLines(@"C:\tmp\sg_creds.txt");
            apiClient = new ApiClient() { ApiToken = creds[0], ApiTokenSecret = creds[1] };
            testStartedAt = DateTime.Now;
        }
    }
}
