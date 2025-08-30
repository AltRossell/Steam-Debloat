export default async function handler(req, res) {
  try {
    // URL raw de tu script PowerShell en GitHub
    const url = "https://raw.githubusercontent.com/AltRossell/Steam-Debloat/main/api/menu.ps1";
    
    // fetch nativo de Node (Vercel soporta fetch global)
    const response = await fetch(url);
    
    if (!response.ok) {
      return res.status(500).send("Error al obtener el script desde GitHub");
    }

    const script = await response.text();

    // Configuramos la respuesta como texto plano
    res.setHeader("Content-Type", "text/plain");
    res.status(200).send(script);
  } catch (error) {
    console.error(error);
    res.status(500).send("Error interno del servidor");
  }
}
