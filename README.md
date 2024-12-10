# grendene-caracois-api-csharp

## Integrações
- [ ] Nodered: http://gra-lxsobcaracol.sob.ad-grendene.com:1880/

## Procedures
- [ ] InserirDesempenho - CaixaBLL
EXEC sp_siag_gestaovisual_gravaperformance @idCaixa, null, @idOperador, @idEquipamento, @idArea, null, 0, 0, @erroClassificacao
- [ ] GerarAtividadePalletCheio - PalletBLL
EXEC sp_siag_criachamada @id_atividade, @id_palletorigem, @id_areaarmazenagemorigem, null, 100100000"

## Tabelas
1. areaarmazenagem
2. caixaleitura
3. caixa
4. equipamento
5. lidervirtual
6. operador
7. pallet
8. parametromensagemcaracol
9. pedido
10. posicaocaracolrefugo
11. turno
# Tabelas sem Model definido (uso diretamente em queries)
12. programa -> GetFabrica -> CaixaBLL
13. agrupadorativo -> GetAgrupadorStatus -> CaixaBLL
14. parametro -> VerificaParametroEmail, GetMeta -> EquipamentoBLL, OperadorBLL
15. atividade -> RotinaLuzVermelha,GerarAtividadePalletCheio -> EquipamentoBLL,PalletBLL
16. chamada -> RotinaLuzVermelha -> EquipamentoBLL
17. status_leitor -> AtualizaStatusLeitorAsync -> EquipamentoBLL
18. operadorhistorico -> GetQtdCaixasPendentesLiderVirtual -> EquipamentoBLL
19. desempenho -> CalcularPerformanceTurnoAtual -> OperadorBLL
20. niveisagrupadores -> GetNivelAgrupador -> PalletBLL
