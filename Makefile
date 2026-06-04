.PHONY: backend-restore backend-build backend-test backend-run frontend-install frontend-run speckit-init

backend-restore:
	dotnet restore BackEnd/RinhaDasLendas.sln

backend-build:
	dotnet build BackEnd/RinhaDasLendas.sln

backend-test:
	dotnet test BackEnd/RinhaDasLendas.sln

backend-run:
	dotnet run --project BackEnd/src/RinhaDasLendas.Api

frontend-install:
	npm --prefix FrontEnd install

frontend-run:
	npm --prefix FrontEnd run dev -- --host 0.0.0.0

speckit-init:
	specify init --here --force --integration codex --integration-options="--skills" --ignore-agent-tools

