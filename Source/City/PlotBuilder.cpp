// Fill out your copyright notice in the Description page of Project Settings.

#include "City.h"
#include "PlotBuilder.h"



// Sets default values
APlotBuilder::APlotBuilder()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;
	houses;
}

// Called when the game starts or when spawned
void APlotBuilder::BeginPlay()
{
	Super::BeginPlay();
	
}

void APlotBuilder::BuildPlot(FPlotPolygon p) {
	//for (AHouseBuilder* h :houses)
	//{
	//	h->Destroy();
	//	delete(h);
	//}

	//houses.Empty();

	for (int i = 1; i < p.f.points.Num(); i++) {
		// one house per segment
		FVector location;
		FActorSpawnParameters spawnInfo;
		location = (p.f.points[i] - p.f.points[i - 1]) / 2 + p.f.points[i - 1];
		FVector offset =  FRotator(0, p.f.buildLeft ? 90 : 270, 0).RotateVector(FVector(2000, 0, 0));
		
		AHouseBuilder* h = GetWorld()->SpawnActor<AHouseBuilder>(location, FRotator(0, 0, 0), spawnInfo);
		FHousePolygon fh;
		FPolygon pol;
		pol.points.Add(p.f.points[i - 1]);
		pol.points.Add(p.f.points[i]);
		pol.points.Add(p.f.points[i] + offset);
		pol.points.Add(p.f.points[i - 1] + offset);
		pol.center = getCenter(pol);
		fh.polygon = pol;
		fh.population = 1.0;
		h->placeHouse(fh);
		houses.Add(h);
		//fh.
		//h->placeHouse()
	}
	//if (p.f.open) {

	//}

}

// Called every frame
void APlotBuilder::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}
