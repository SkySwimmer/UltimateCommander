package org.asf.linkr;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.ConcurrentModificationException;
import java.util.HashMap;
import java.util.UUID;

import org.asf.cyan.api.common.CYAN_COMPONENT;
import org.asf.rats.Memory;
import org.asf.rats.ModuleBasedConfiguration;

@CYAN_COMPONENT
public class LinkrModule extends LinkrModificationManager {

	private ArrayList<byte[]> output = new ArrayList<byte[]>();
	private ArrayList<IntentResult> results = new ArrayList<IntentResult>();

	private static LinkrModule inst;

	public static LinkrModule getInstance() {
		return inst;
	}

	public class IntentResult {
		public byte result = 0;
		public String address = "";
		public byte[] payload;
	}

	private static HashMap<String, String> configuration = new HashMap<String, String>();
	private static boolean hasConfigChanged = false;

	@Override
	protected String moduleId() {
		return "linkr-http";
	}

	static {
		if (System.getProperty("os.name").toLowerCase().contains("win")
				&& !System.getProperty("os.name").toLowerCase().contains("darawin")) {
			configuration.put("bot-link-input", "\\\\.\\pipe\\link_r_backendlink.pipein");
			configuration.put("bot-link-output", "\\\\.\\pipe\\link_r_backendlink.pipeout");
		} else {
			configuration.put("bot-link-input", "");
			configuration.put("bot-link-output", "");
		}
	}

	// Called on module loading
	protected static void initComponent() {
		assign(new LinkrModule());
		LinkrModule.start();
	}

	private byte[] readNBytes(InputStream strm, int count) throws IOException {
		ByteBuffer buf = ByteBuffer.allocate(count);
		for (int i = 0; i < count; i++) {
			int d = strm.read();
			if (d < 0)
				throw new IOException("Stream closed");
			buf.put((byte) d);
		}
		return buf.array();
	}

	@Override
	protected void startModule() {
		inst = this;
		Memory.getInstance().getOrCreate("bootstrap.call").<Runnable>append(() -> readConfig());
		Memory.getInstance().getOrCreate("bootstrap.reload").<Runnable>append(() -> readConfig());

		Thread th = new Thread(() -> {
			while (true) {
				while (configuration.get("bot-link-input").isEmpty()) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}
				while (!new File(configuration.get("bot-link-input")).exists()) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}
				while (output.size() == 0)
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}

				ArrayList<byte[]> packets;
				while (true) {
					try {
						packets = new ArrayList<byte[]>(output);
						break;
					} catch (ConcurrentModificationException e) {
					}
				}

				try {
					for (byte[] packet : packets) {
						FileOutputStream strm = new FileOutputStream(configuration.get("bot-link-input"));
						byte[] pref = ByteBuffer.allocate(4).putInt(packet.length).array();
						strm.write(pref);
						strm.write(packet);
						strm.close();
						output.remove(packet);
					}
				} catch (IOException e) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}
			}
		}, "Link/R Packet Writer");
		th.setDaemon(true);
		th.start();

		th = new Thread(() -> {
			while (true) {
				while (configuration.get("bot-link-output").isEmpty()) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}
				while (!new File(configuration.get("bot-link-output")).exists()) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}

				try {
					FileInputStream strm = new FileInputStream(new File(configuration.get("bot-link-output")));
					byte[] pref = readNBytes(strm, 4);
					byte[] packet = readNBytes(strm, ByteBuffer.wrap(pref).getInt());
					strm.close();

					ByteArrayInputStream strm2 = new ByteArrayInputStream(packet);
					IntentResult res = new IntentResult();
					res.result = (byte) strm2.read();
					res.address = new String(strm2.readNBytes(ByteBuffer.wrap(strm2.readNBytes(4)).getInt()), "UTF-8");
					res.payload = new byte[0];
					if (res.result == 0)
						res.payload = strm2.readNBytes(ByteBuffer.wrap(strm2.readNBytes(4)).getInt());
					if (results.size() + 100 >= Integer.MAX_VALUE) {
						results.remove(0);
					}
					results.add(res);
					strm2.close();
				} catch (IOException e) {
					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}
			}
		}, "Link/R Packet Reader");
		th.setDaemon(true);
		th.start();
	}

	public IntentResult sendCommand(long domain, long subdomain, String address, String intent, String parameters,
			byte[] payload) {
		try {
			for (int i = 0; i < 100; i++) {
				if (configuration.get("bot-link-output").isEmpty() || configuration.get("bot-link-input").isEmpty()
						|| !new File(configuration.get("bot-link-output")).exists()
						|| !new File(configuration.get("bot-link-input")).exists()) {
					IntentResult r = new IntentResult();
					r.payload = new byte[0];
					r.result = 5;
					r.address = address;
					return r;
				}

				ByteArrayOutputStream strm = new ByteArrayOutputStream();
				strm.write(0);
				strm.write(ByteBuffer.allocate(8).putLong(domain).array());
				strm.write(ByteBuffer.allocate(8).putLong(subdomain).array());

				byte[] d = address.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				d = intent.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				d = parameters.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				d = payload;
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				output.add(strm.toByteArray());
				strm.close();

				for (int i2 = 0; i2 < 100; i2++) {
					ArrayList<IntentResult> resultAr;
					while (true) {
						try {
							resultAr = new ArrayList<IntentResult>(results);
							break;
						} catch (ConcurrentModificationException e) {
						}
					}

					for (IntentResult r : resultAr) {
						if (r.address.equals(address)) {
							results.remove(r);
							return r;
						}
					}

					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}

				try {
					Thread.sleep(1000);
				} catch (InterruptedException e1) {
				}
			}
		} catch (IOException e) {
		}

		IntentResult r = new IntentResult();
		r.payload = new byte[0];
		r.result = 1;
		r.address = address;
		return r;
	}

	public IntentResult sendSpinCommand(long subdomain, String token, String intent) {
		String resID = (token + UUID.randomUUID() + System.currentTimeMillis());
		try {
			for (int i = 0; i < 100; i++) {
				if (configuration.get("bot-link-output").isEmpty() || configuration.get("bot-link-input").isEmpty()
						|| !new File(configuration.get("bot-link-output")).exists()
						|| !new File(configuration.get("bot-link-input")).exists()) {
					IntentResult r = new IntentResult();
					r.payload = new byte[0];
					r.result = 5;
					r.address = token;
					return r;
				}

				ByteArrayOutputStream strm = new ByteArrayOutputStream();
				strm.write(1);

				byte[] d = token.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);
				
				strm.write(ByteBuffer.allocate(8).putLong(subdomain).array());

				d = intent.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				d = resID.getBytes("UTF-8");
				strm.write(ByteBuffer.allocate(4).putInt(d.length).array());
				strm.write(d);

				output.add(strm.toByteArray());
				strm.close();

				for (int i2 = 0; i2 < 100; i2++) {
					ArrayList<IntentResult> resultAr;
					while (true) {
						try {
							resultAr = new ArrayList<IntentResult>(results);
							break;
						} catch (ConcurrentModificationException e) {
						}
					}

					for (IntentResult r : resultAr) {
						if (r.address.equals(resID)) {
							results.remove(r);
							return r;
						}
					}

					try {
						Thread.sleep(100);
					} catch (InterruptedException e1) {
					}
				}

				try {
					Thread.sleep(1000);
				} catch (InterruptedException e1) {
				}
			}
		} catch (IOException e) {
		}

		IntentResult r = new IntentResult();
		r.payload = new byte[0];
		r.result = 1;
		r.address = token;
		return r;
	}

	private void readConfig() {
		hasConfigChanged = false;

		ModuleBasedConfiguration<?> config = Memory.getInstance().get("memory.modules.shared.config")
				.getValue(ModuleBasedConfiguration.class);

		HashMap<String, String> ourConfigCategory = config.modules.getOrDefault(moduleId(),
				new HashMap<String, String>());

		if (!config.modules.containsKey(moduleId())) {
			ourConfigCategory.putAll(configuration);
			hasConfigChanged = true;

		} else {
			configuration.forEach((key, value) -> {
				if (!ourConfigCategory.containsKey(key)) {
					hasConfigChanged = true;
					ourConfigCategory.put(key, value);
				} else {
					configuration.put(key, ourConfigCategory.get(key));
				}

			});
		}

		config.modules.put(moduleId(), ourConfigCategory);
		if (hasConfigChanged) {
			try {
				config.writeAll();
			} catch (IOException e) {
				error("Config saving failed!", e);
			}
		}
	}
}
