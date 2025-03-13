package org.asf.linkr;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.UnsupportedEncodingException;
import java.net.Socket;
import java.net.URLDecoder;
import java.util.HashMap;
import java.util.Map;

import org.asf.rats.ConnectiveHTTPServer;
import org.asf.rats.processors.HttpUploadProcessor;

public class LinkrUploadProcessor extends HttpUploadProcessor {

	@Override
	public HttpUploadProcessor createNewInstance() {
		return new LinkrUploadProcessor();
	}

	@Override
	public String path() {
		return "/linkr/interface";
	}

	@Override
	public boolean supportsGet() {
		return true;
	}

	public static Map<String, String> parseQuery(String query) {

		HashMap<String, String> map = new HashMap<String, String>();

		String key = "";
		String value = "";
		boolean isKey = true;

		for (int i = 0; i < query.length(); i++) {
			char ch = query.charAt(i);
			if (ch == '&' || ch == '?') {
				if (isKey && !key.isEmpty()) {
					map.put(key, "");
					key = "";
				} else if (!isKey && !key.isEmpty()) {
					try {
						map.put(key, URLDecoder.decode(value, "UTF-8"));
					} catch (UnsupportedEncodingException e) {
						map.put(key, value);
					}
					isKey = true;
					key = "";
					value = "";
				}
			} else if (ch == '=') {
				isKey = !isKey;
			} else {
				if (isKey) {
					key += ch;
				} else {
					value += ch;
				}
			}
		}
		if (!key.isEmpty() || !value.isEmpty()) {
			try {
				map.put(key, URLDecoder.decode(value, "UTF-8"));
			} catch (UnsupportedEncodingException e) {
				map.put(key, value);
			}
		}

		return map;
	}

	@Override
	public void process(String arg0, Socket arg1, String method) {
		if (method.equals("POST") || method.equals("GET")) {
			Map<String, String> query = parseQuery(getRequest().query);
			if (!query.containsKey("subsystemaddress") || !query.containsKey("intenttoken")
					|| !query.containsKey("domain") || !query.containsKey("subdomain")
					|| !query.get("domain").matches("^[0-9]+") || !query.get("subdomain").matches("^[0-9]+")
					|| !query.containsKey("parameters")) {
				if (method.equals("POST") && query.containsKey("method") && query.containsKey("runonsubdomain")
					&& query.get("runonsubdomain").matches("^[0-9]+") && query.containsKey("intent")
					&& query.get("method").equals("spin")) {
					String token = this.getRequestBody().replace("\n", "").replace("\r", "");
					
					LinkrModule.IntentResult res = LinkrModule.getInstance().sendSpinCommand(
						Long.parseLong(query.get("runonsubdomain")), token, query.get("intent"));

					if (res.result == 0) {
						getResponse().body = new ByteArrayInputStream(res.payload);
						getResponse().setHeader("Content-Type", "application/json");
						setResponseMessage("OK");
						setResponseCode(200);
					} else if (res.result == 1) {
						setResponseCode(500);
						setResponseMessage("Internal Server Error");
						this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
					} else if (res.result == 5) {
						setResponseCode(500);
						setResponseMessage("Internal Server Error: server is not configured");
						this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
					} else if (res.result == 2) {
						setResponseCode(400);
						setResponseMessage("Bad Request");
						this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
					} else if (res.result == 3) {
						setResponseCode(400);
						setResponseMessage("Bad Request");
						this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
					} else if (res.result == 4) {
						setResponseCode(404);
						setResponseMessage("Not Found");
						this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
					}
					return;
				}
				this.setResponseCode(400);
				this.setResponseMessage("Bad Request");
				this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
			} else {
				byte[] data = new byte[0];
				if (method.equals("POST")) {
					ByteArrayOutputStream strm = new ByteArrayOutputStream();
					try {
						ConnectiveHTTPServer.transferRequestBody(getHeaders(), getRequestBodyStream(), strm);
					} catch (IOException e) {
					}
					data = strm.toByteArray();
				}

				LinkrModule.IntentResult res = LinkrModule.getInstance().sendCommand(
						Long.parseLong(query.get("domain")), Long.parseLong(query.get("subdomain")),
						query.get("subsystemaddress"), query.get("intenttoken"), query.get("parameters"), data);

				if (res.result == 0) {
					getResponse().body = new ByteArrayInputStream(res.payload);
					getResponse().setHeader("Content-Type", "application/json");
					setResponseMessage("OK");
					setResponseCode(200);
				} else if (res.result == 1) {
					setResponseCode(500);
					setResponseMessage("Internal Server Error");
					this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
				} else if (res.result == 5) {
					setResponseCode(500);
					setResponseMessage("Internal Server Error: server is not configured");
					this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
				} else if (res.result == 2) {
					setResponseCode(400);
					setResponseMessage("Bad Request");
					this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
				} else if (res.result == 3) {
					setResponseCode(400);
					setResponseMessage("Bad Request");
					this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
				} else if (res.result == 4) {
					setResponseCode(404);
					setResponseMessage("Not Found");
					this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
				}
			}
		} else {
			// Deny other methods
			this.setResponseCode(405);
			this.setResponseMessage(method.toUpperCase() + " is not supported.");
			this.setBody("text/html", getServer().genError(getResponse(), getRequest()));
		}
	}

}
