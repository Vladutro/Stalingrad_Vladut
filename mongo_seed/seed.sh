#!/bin/bash
mongoimport --uri ${DATABASE_URI} --db ${DATABASE_NAME} --collection tanks --drop --file tanks.json --stopOnError
mongoimport --uri ${DATABASE_URI} --db ${DATABASE_NAME} --collection maps --drop --file maps.json --stopOnError
mongoimport --uri ${DATABASE_URI} --db ${DATABASE_NAME} --collection scores --drop --file scores.json --stopOnError
