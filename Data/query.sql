--B7075,B7956
exec usp_gre_enriquecer_detalle_bien 'B7075'
exec usp_gre_enriquecer_detalle_bien 'B7956' -- con la partida se obtiene el nombre comercial, el codigo de tela, la orden de compra y el codigo de proveedor
exec usp_gre_enriquecer_orden_compra '100-185191' -- con cod de clase de orden de compra  y centro de costo
exec usp_gre_mov_x_clase '06' -- con cod de clase de orden de compra se obtiene el movimiento

create table gre_estado (idEstado int identity(1,1) primary key, descripcion varchar(100) not null)
create table gre_urls(idURL int identity(1,1) primary key, url varchar(255) not null,descripcion varchar(255))


insert into gre_urls (url, descripcion) values ('http://localhost:5133','prueba')
insert into gre_estado (descripcion) values ('PENDIENTE'),('OBSERVADO')


create or alter procedure usp_gre_enriquecer_detalle_bien
@cod_ordtra cod_ordtra
as 
begin
	if isnull(@cod_ordtra,'') =''
		THROW 51000, 'Ingrese la partida.', 1;


	select [nombre_comercial] = isnull(c.Des_Tela_Comercial,''),
		[cod_tela]=isnull(a.Cod_Tela,''),
		[oc]=case when a.Cod_Ordcomp <> '' then  isnull(a.Ser_OrdComp,'') + '-' + isnull(a.Cod_Ordcomp,'')
				else ''
				end
	FROM ti_ordtra_tintoreria_items a 
		INNER JOIN ti_ordtra_tintoreria b      
	ON a.Cod_Ordtra          = b.Cod_Ordtra      
		INNER JOIN Tx_Tela c      
	ON a.Cod_Tela            = c.Cod_Tela      
		where a.Cod_Ordtra = @cod_ordtra
end;


create or alter procedure usp_gre_enriquecer_orden_compra
@orden_compra varchar(10)= null
as
begin
	if isnull(@orden_compra,'') =''
		THROW 51000, 'Ingrese la orden de compra.', 1;

	select [clase_orden]=Cod_ClaOrdComp,
		[centro_costo]=rtrim(ltrim(Cod_CenCost)),
		[cod_proveedor]=rtrim(ltrim(Cod_Proveedor)),
		[estado_orden]=Cod_StaOrdComp
	from Lg_OrdComp where Ser_OrdComp +'-'+ Cod_OrdComp =@orden_compra
end;

create or alter procedure usp_gre_mov_x_clase @Cod_ClaOrdComp Cod_ClaOrdComp
as
begin
	if isnull(@Cod_ClaOrdComp,'')=''
		THROW 51000, 'Ingrese el codigo de orden de clase.', 1;

	select [movimiento] = cod_tipmov
	from gre_mov_x_clase
	where Cod_ClaOrdComp = @Cod_ClaOrdComp
end;







