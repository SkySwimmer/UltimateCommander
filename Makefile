compile:
	@./build.sh
apply:
	@./applypatches.sh
buildpatches:
	@./buildpatches.sh
clean:
	@./clean.sh
run:
	@(cd work/build; ./cmd-r)
install:
	@(cd work ; sudo base-project/lib/do-install-c.sh base-project)
uninstall:
	@(cd work ; sudo base-project/lib/do-uninstall-c.sh base-project)
remove:
	@(cd work ; sudo base-project/lib/do-uninstall-c.sh base-project)
