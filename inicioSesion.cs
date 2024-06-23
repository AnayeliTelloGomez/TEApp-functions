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
//creacion token
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace TEAPP
{
    public static class inicioSesion
    {
        class UsuarioInicio{
            public string correo;
            public string contrasena;
            public string tipo;
        }

        class ParamInicio{
            public UsuarioInicio usuarioInicio;
        }

        class Error
        {
            public string mensaje;
            public Error(string mensaje)
            {
                this.mensaje = mensaje;
            }
        }

        [FunctionName("inicioSesion")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            ILogger log)
        {
            try{
                log.LogInformation("Inicio de sesión.");
                //leer la solicitud http y almacenarlo en una cadena
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ParamInicio data = JsonConvert.DeserializeObject<ParamInicio>(requestBody);
                UsuarioInicio usuario = data.usuarioInicio;

                if (usuario.correo.Equals("") || usuario.correo == null) throw new Exception("Ingresar correo electrónico.");
                if (usuario.contrasena.Equals("") || usuario.contrasena == null) throw new Exception("Ingresar contraseña.");
                
                string hashedPassword = encode(usuario.contrasena);

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

                MySqlTransaction transaccion = conexion.BeginTransaction();

                try
                {
                    var cmdInicio = new MySqlCommand();
                    cmdInicio.Connection = conexion;
                    //cmdInicio.Transaction = transaccion;
                    
                    object resultado = new object();
                    
                    if (usuario.tipo.Equals("2")){
                        cmdInicio.CommandText = "select * from paciente where correo=@correo and contrasena=@contrasena";
                        cmdInicio.Parameters.AddWithValue("@correo", usuario.correo);
                        cmdInicio.Parameters.AddWithValue("@contrasena", hashedPassword);
                        resultado = cmdInicio.ExecuteScalar();
                    }
                    else if(usuario.tipo.Equals("1")){
                        cmdInicio.CommandText = "select * from especialista where correo=@correo and contrasena=@contrasena";
                        cmdInicio.Parameters.AddWithValue("@correo", usuario.correo);
                        cmdInicio.Parameters.AddWithValue("@contrasena", hashedPassword);
                        resultado = cmdInicio.ExecuteScalar();
                        int validado=0;
                        using (MySqlDataReader reader= cmdInicio.ExecuteReader()){
                            while (reader.Read()){
                                validado=reader.GetInt32(reader.GetOrdinal("idadministrador"));
                            }
                        }
                        if (validado == 1){
                            return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Pida a su responsable que lo valide.")));
                        }
                    }else{
                        cmdInicio.CommandText = "select * from administrador where correo=@correo and contrasena=@contrasena";
                        cmdInicio.Parameters.AddWithValue("@correo", usuario.correo);
                        cmdInicio.Parameters.AddWithValue("@contrasena", hashedPassword);
                        resultado = cmdInicio.ExecuteScalar();
                    }
                    if (resultado != null && Convert.ToInt32(resultado) > 0){
                        return new OkObjectResult(new { message=GenerateJwtToken(usuario.correo,usuario.tipo)});
                    }else{
                        return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error("Credenciales erroneas. correo"+usuario.tipo+"")));
                    }
                }catch (Exception e){
                    throw new Exception(e.Message);
                }
                finally{
                    conexion.Close();
                }
            }catch (Exception e){
                Console.WriteLine(e.Message);
                return new BadRequestObjectResult(JsonConvert.SerializeObject(new Error(e.Message +"error global")));
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

            static string GenerateJwtToken(string correo, string role)
            {
                //cambiar a variables de entorno
                string issuer=Environment.GetEnvironmentVariable("proveedor");
                string audience=Environment.GetEnvironmentVariable("audiencia");
                int expiryMinutes=30;
                //string secretKey=Environment.GetEnvironmentVariable("salt");
                string secretKey = Environment.GetEnvironmentVariable("secretKey");
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(secretKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("correo", correo),
                        new Claim("role", role)
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
        }
    }
}
