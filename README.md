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
