using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;
using Org.BouncyCastle.Math.EC.Rfc7748;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Transactions;
using MySql.Data.MySqlClient;

namespace TEAPP
{
    public static class codigoValidar
    {
        [FunctionName("codigoValidar")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
            log.LogInformation("C# HTTP trigger function processed a request.");

            string correo = req.Query["correo"];
            string email=Environment.GetEnvironmentVariable("correo");
            string password=Environment.GetEnvironmentVariable("passCorreo");
            string myAlias="TEApp";
            MailMessage mCorreo;
            mCorreo=new MailMessage();
            mCorreo.From=new MailAddress(email,myAlias,System.Text.Encoding.UTF8);
            mCorreo.Subject="Recuperar contraseña";
            mCorreo.To.Add(correo);
            Random random = new Random();
            int codigo = random.Next(100000, 999999);
            mCorreo.Body=@"
                <html>
                <head>
                    <style>
                        /* Estilos CSS para el cuerpo del mensaje */
                        body {
                            font-family: Arial, sans-serif;
                            background-color: #f4f4f4;
                            padding: 20px;
                        }
                        .container {
                            max-width: 600px;
                            margin: 0 auto;
                            background-color: #ffffff;
                            padding: 20px;
                            border-radius: 10px;
                            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
                        }
                        h1 {
                            color: #333333;
                            text-align: center;
                        }
                        p {
                            color: #666666;
                        }
                        .code {
                            background-color: #f9f9f9;
                            border: 1px solid #dddddd;
                            border-radius: 5px;
                            padding: 10px;
                            text-align: center;
                            font-size: 15px;
                            margin-top: 20px;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>Código de verificación</h1>
                        <p>Estimado usuario,</p>
                        <p>Su código de validación es:</p>
                        <div class='code'>TEApp-"+codigo+@"</div>
                        <p>Utilice este código para validar su identidad.</p>
                        <p>Gracias,<br/>Equipo de TEApp</p>
                    </div>
                </body>
                </html>";
            mCorreo.IsBodyHtml=true;
            mCorreo.Priority=MailPriority.Normal;
            SmtpClient smtp=new SmtpClient();
            smtp.UseDefaultCredentials=false;
            smtp.Port=587;
            smtp.Host="smtp.gmail.com";
            smtp.Credentials=new System.Net.NetworkCredential(email,password);
            ServicePointManager.ServerCertificateValidationCallback=delegate(object s, 
            X509Certificate certificate, X509Chain chain, SslPolicyErrors error) 
            {return true;};
            smtp.EnableSsl = true;
            smtp.Send(mCorreo);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            correo = correo ?? data?.correo;
            //paramatremos para conectar con la base
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string DB = Environment.GetEnvironmentVariable("DataBase");
                //Crear cadena de conexion
                string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";Database=" + DB + ";SslMode=Preferred;";
                //crear la conexion
                var conexion = new MySqlConnection(conDB);
                //abrir conexion
                conexion.Open();
            try{
                    var cmdAltaPaciente = new MySqlCommand();
                    cmdAltaPaciente.Connection = conexion;
                    cmdAltaPaciente.CommandText = "UPDATE especialista SET validador=@codigo where correo=@correo";
                    cmdAltaPaciente.Parameters.AddWithValue("@correo",correo);
                    cmdAltaPaciente.Parameters.AddWithValue("@validador",codigo);
                }catch (Exception e){
                    //throw new Exception(e.Message);
                    return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message +"error global")));
                }
                finally{
                    conexion.Close();
                }
            return new OkObjectResult("Correo enviado exitosamente");
        }catch (Exception e){
            Console.WriteLine(e.Message);
            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message +"error global")));
        }
    }
}
}
