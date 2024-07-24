import React from 'react';
import Carousel from 'react-multi-carousel';
import 'react-multi-carousel/lib/styles.css';
import { ISpecialization } from '../../libs/models/Specialization';
import { SpecializationCard } from './SpecializationCard';

const responsive = {
    // Define responsive settings for different screen sizes
    desktop: {
        breakpoint: { max: 3000, min: 1024 },
        items: 2,
        slidesToSlide: 2,
    },
    tablet: {
        breakpoint: { max: 1024, min: 464 },
        items: 2,
        slidesToSlide: 2,
    },
    mobile: {
        breakpoint: { max: 464, min: 0 },
        items: 1,
        slidesToSlide: 1,
    },
};

interface SpecializationProps {
    /* eslint-disable 
      @typescript-eslint/no-unsafe-assignment
    */
    specializations: ISpecialization[];
    setShowSpecialization: any;
}

export const SpecializationCardList: React.FC<SpecializationProps> = ({ specializations, setShowSpecialization }) => {
    return (
        <>
            <div>
                <Carousel responsive={responsive}>
                    {specializations.map((_specialization, index) => (
                        <>
                            <div className="root">
                                <SpecializationCard
                                    key={index}
                                    specialization={_specialization}
                                    setShowSpecialization={setShowSpecialization}
                                />
                            </div>
                        </>
                    ))}
                </Carousel>
            </div>
        </>
    );
};
