export default async function handler(req, res) {
	try {
		const url =
			"https://raw.githubusercontent.com/AltRossell/Steam-Debloat/main/script/app.ps1";

		const response = await fetch(url);

		if (!response.ok) {
			return res.status(500).send("Error");
		}

		const script = await response.text();

		res.setHeader("Content-Type", "text/plain");
		res.status(200).send(script);
	} catch (error) {
		console.error(error);
		res.status(500).send("Error");
	}
}
