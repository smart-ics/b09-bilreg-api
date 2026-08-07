CREATE TABLE FARPU_KfaIngredient
(
	KfaId VARCHAR(10) NOT NULL CONSTRAINT FARPU_KfaIngredient_KfaId DEFAULT(''),
	KfaIngredientId VARCHAR(10) NOT NULL CONSTRAINT FARPU_KfaIngredient_KfaIngredientId DEFAULT(''),
    KfaIngredientName VARCHAR(512) NOT NULL CONSTRAINT FARPU_KfaIngredient_KfaIngredientName DEFAULT(''), 
	Active BIT NOT NULL CONSTRAINT FARPU_KfaIngredient_Active DEFAULT(0),
	State VARCHAR(10) NOT NULL CONSTRAINT FARPU_KfaIngredient_State DEFAULT(''), 
	Strength VARCHAR(30) NOT NULL CONSTRAINT FARPU_KfaIngredient_Strength DEFAULT(''), 

	CONSTRAINT PK_FARPU_KfaIngredient_KfaId PRIMARY KEY CLUSTERED(KfaId, KfaIngredientId)
)