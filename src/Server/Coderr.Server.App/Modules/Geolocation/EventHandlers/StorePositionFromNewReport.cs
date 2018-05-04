﻿using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using codeRR.Server.Api.Core.Incidents.Events;
using DotNetCqs;
using Griffin.Container;
using log4net;
using Newtonsoft.Json.Linq;

namespace codeRR.Server.App.Modules.Geolocation.EventHandlers
{
    /// <summary>
    ///     Responsible of looking up geographic position of the IP address that delivered the report.
    /// </summary>
    [Component(RegisterAsSelf = true)]
    public class StorePositionFromNewReport : IMessageHandler<ReportAddedToIncident>
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(StorePositionFromNewReport));
        private readonly IErrorOriginRepository _repository;

        /// <summary>
        ///     Creates a new instance of <see cref="StorePositionFromNewReport" />.
        /// </summary>
        /// <param name="repository">repos</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public StorePositionFromNewReport(IErrorOriginRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        /// <summary>
        ///     Process an event asynchronously.
        /// </summary>
        /// <param name="e">event to process</param>
        /// <returns>
        ///     Task to wait on.
        /// </returns>
        public async Task HandleAsync(IMessageContext context, ReportAddedToIncident e)
        {
            if (string.IsNullOrEmpty(e.Report.RemoteAddress))
                return;

            if (e.Report.RemoteAddress == "::1")
                return;
            if (e.Report.RemoteAddress == "127.0.0.1")
                e.Report.RemoteAddress = "94.254.57.227";

            var request = WebRequest.CreateHttp("http://freegeoip.net/json/" + e.Report.RemoteAddress);
            string json = "";
            try
            {
                var response = await request.GetResponseAsync();
                var stream = response.GetResponseStream();
                var reader = new StreamReader(stream);
                json = await reader.ReadToEndAsync();
                var jsonObj = JObject.Parse(json);

                /*    /*{"ip":"94.254.21.175","country_code":"SE","country_name":"Sweden","region_code":"10","region_name":"Dalarnas Lan","city":"Falun","zipcode":"",
                 * "latitude":60.6,"longitude":15.6333,
     * "metro_code":"","areacode":""}*/

                var lat = double.Parse(jsonObj["latitude"].Value<string>(), CultureInfo.InvariantCulture);
                var lon = double.Parse(jsonObj["longitude"].Value<string>(), CultureInfo.InvariantCulture);
                var cmd = new ErrorOrigin(e.Report.RemoteAddress, lon, lat)
                {
                    City = jsonObj["city"].ToString(),
                    CountryCode = jsonObj["country_code"].ToString(),
                    CountryName = jsonObj["country_name"].ToString(),
                    RegionCode = jsonObj["region_code"].ToString(),
                    RegionName = jsonObj["region_name"].ToString(),
                    ZipCode = jsonObj["zip_code"].ToString()
                };

                await _repository.CreateAsync(cmd, e.Incident.ApplicationId, e.Incident.Id, e.Report.Id);
            }
            catch (Exception exception)
            {
                _logger.Error($"Failed to store location for incident {e.Incident.Id}/report {e.Report.Id}: {json}", exception);
            }
        }
    }
}