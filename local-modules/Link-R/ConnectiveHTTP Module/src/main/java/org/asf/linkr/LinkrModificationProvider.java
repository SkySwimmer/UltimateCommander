package org.asf.linkr;

import java.io.IOException;

import org.asf.rats.http.IAutoContextModificationProvider;
import org.asf.rats.http.ProviderContextFactory;

// This class allows RaTs! to use the module components, it allows
// for modifying any context created by the IAutoContextBuilders used by RaTs.
public class LinkrModificationProvider implements IAutoContextModificationProvider {

	@Override
	public void accept(ProviderContextFactory arg0) {
		if (!LinkrModificationManager.hasBeenPrepared()) {
			try {
				LinkrModificationManager.prepareModifications();
			} catch (IOException e) {
				throw new RuntimeException(e);
			}
		}

		// Apply modifications
		LinkrModificationManager.appy(arg0);
	}

}
