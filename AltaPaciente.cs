using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
//cifrado contrasena
using System.Security.Cryptography;
using System.Text;


namespace TEAPP
{
    public static class AltaPaciente
    {
        class Paciente
        {
            public string correo;
            public string contrasena;
            public string nombres;
            public string paterno;
            public string materno;
            public string tipo;
        }

        class ParamAltaPaciente
        {
            public Paciente paciente;
        }

        //clase para crear el error y su constructor
        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }
        [FunctionName("AltaPaciente")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("Alta usuario.");
                //leer la solicitud HTTP y almacenar en una cadena
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //pasar la cadena leída en la solicitud HTTP a un objeto ripo paramAltaPaciente
                log.LogInformation("reqbody", requestBody);
                ParamAltaPaciente data = JsonConvert.DeserializeObject<ParamAltaPaciente>(requestBody);
                //Crear al objeto paciente
                Paciente paciente = data.paciente;

                if (data == null) throw new Exception("No se pudo deserializar la solicitud.");
                log.LogInformation("nombre: ", paciente.nombres);
                //Verificar los parametros
                if (paciente.nombres == null || paciente.nombres == "") throw new Exception("Se debe ingresar al menos un nombre.");
                if (paciente.paterno == null || paciente.paterno == "") throw new Exception("Se debe ingresar el apellido paterno");
                if (paciente.materno == null || paciente.materno == "") throw new Exception("Se debe ingresar el apellido materno");
                if (paciente.correo == null || paciente.correo == "") throw new Exception("Se debe ingresar un correo electrónico");
                if (paciente.contrasena == null || paciente.contrasena == "") throw new Exception("Se debe ingresar el apelliuna contraseña");

                string hashedPassword = encode(paciente.contrasena);
                //leer los parametros para establecer conexión
                string Server = Environment.GetEnvironmentVariable("Server");
                string UserID = Environment.GetEnvironmentVariable("UserID");
                string Password = Environment.GetEnvironmentVariable("Password");
                string DB = Environment.GetEnvironmentVariable("DataBase");

                //Crear string de conexion a BD
                string conDB = "Server=" + Server + ";UserID=" + UserID + ";Password=" + Password + ";" + "Database=" + DB + ";SslMode=Preferred;";
                //crear conex[on]
                var conexion = new MySqlConnection(conDB);
                //abrir conexion
                conexion.Open();

                //iniciar transaccion para asegurar integridad de datos
                MySqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    //Crear la variable tipo comando en MySQL y asignarle sus caracteristicas
                    var cmdAltaPaciente = new MySqlCommand();
                    cmdAltaPaciente.Connection = conexion;
                    cmdAltaPaciente.Transaction = transaccion;
                    if (paciente.tipo.Equals("2"))
                    {
                        cmdAltaPaciente.CommandText = "insert into paciente (idpaciente,correo,contrasena,nombres,paterno,materno,idespecialista) values (0,@correo,@contrasena,@nombres,@paterno,@materno,1j)";
                        cmdAltaPaciente.Parameters.AddWithValue("@correo", paciente.correo);
                        cmdAltaPaciente.Parameters.AddWithValue("@contrasena", hashedPassword);
                        cmdAltaPaciente.Parameters.AddWithValue("@nombres", paciente.nombres);
                        cmdAltaPaciente.Parameters.AddWithValue("@paterno", paciente.paterno);
                        cmdAltaPaciente.Parameters.AddWithValue("@materno", paciente.materno);
                    }
                    else
                    {
                        cmdAltaPaciente.CommandText = "insert into especialista (idespecialista,correo,contrasena,nombres,paterno,materno,idadministrador) values (0,@correo,@contrasena,@nombres,@paterno,@materno, 1)";
                        cmdAltaPaciente.Parameters.AddWithValue("@correo", paciente.correo);
                        cmdAltaPaciente.Parameters.AddWithValue("@contrasena", hashedPassword);
                        cmdAltaPaciente.Parameters.AddWithValue("@nombres", paciente.nombres);
                        cmdAltaPaciente.Parameters.AddWithValue("@paterno", paciente.paterno);
                        cmdAltaPaciente.Parameters.AddWithValue("@materno", paciente.materno);
                    }

                    //executar Query (non porque no devuelve datos)
                    cmdAltaPaciente.ExecuteNonQuery();

                    transaccion.Commit();
                    return new OkObjectResult("Usuario registrado correctamente.");

                }
                catch (Exception e)
                {
                    transaccion.Rollback();
                    throw new Exception(e.Message);
                }
                finally
                {
                    conexion.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message)));
            }


            static string encode(string pass){
                string salt = Environment.GetEnvironmentVariable("salt");
                byte[] combinedBytes = Encoding.UTF8.GetBytes(pass + salt);

                // Calcular el hash con SHA-256
                byte[] hashBytes;
                using (SHA256 sha256 = SHA256.Create())
                {
                    hashBytes = sha256.ComputeHash(combinedBytes);
                }

                // Convertir el hash a una cadena hexadecimal
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return hash;
            }
        }
    }
}
